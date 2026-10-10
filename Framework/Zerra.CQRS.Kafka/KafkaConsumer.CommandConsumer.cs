// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System.Collections.Concurrent;
using Zerra.Collections;
using System.Security.Claims;
using System.Text;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        private sealed class CommandConsumer : IDisposable, IAsyncDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter? commandCounter;
            private readonly int? maxPollIntervalMs;
            private readonly TimeSpan busyPollInterval;
            private readonly string topic;
            private readonly string clientID;
            private readonly ConcurrentDictionary<string, Type> commandTypes;
            private readonly Zerra.Serialization.ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ICompressor? compressor;
            private readonly ILogger? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);
            private Task? listening;
            private ProducerConfig? ackProducerConfig;
            private AckProducer? ackProducer;

            //librdkafka keeps every topic a producer has sent to for the producer's life, so after this many acknowledgement topics it's replaced
            private const int maxAckTopicsPerProducer = 1000;
            private const int maxAckAttempts = 12;

            //resilient commands commit a partition's offset up to the oldest command still being handled from it, handlers finish out of order
            private readonly Dictionary<TopicPartition, PartitionOffsets>? offsets;
            private static readonly TimeSpan resilientPollInterval = TimeSpan.FromMilliseconds(100);

            private sealed class PartitionOffsets
            {
                public readonly SortedSet<long> Handling = new();
                public long Next;
                public long Committed = -1;
            }

            private sealed class AckProducer
            {
                public readonly IProducer<string, byte[]> Producer;
                public readonly ConcurrentDictionary<string, byte> Topics = new();
                public int TopicCount;
                public int Using;
                public int Retired;
                private int disposed;

                public AckProducer(IProducer<string, byte[]> producer) => this.Producer = producer;

                //a replaced producer is disposed by whichever reply finishes using it last
                public void Release()
                {
                    if (Interlocked.Decrement(ref Using) == 0 && Volatile.Read(ref Retired) == 1)
                        Dispose();
                }

                public void Dispose()
                {
                    if (Interlocked.Exchange(ref disposed, 1) == 0)
                        Producer.Dispose();
                }
            }

            public CommandConsumer(int maxConcurrent, CommandCounter? commandCounter, string topic, ConcurrentDictionary<string, Type> commandTypes, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync, int? maxPollIntervalMs, bool resilient)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter is not null ? Math.Min(commandCounter.ReceiveCountBeforeExit, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(KafkaConsumer)} truncated the command topic to {KafkaCommon.TopicMaxLength} characters: {this.topic}. Another topic truncating to the same name would be consumed as this one.");
                this.clientID = Environment.MachineName;
                this.commandTypes = commandTypes;
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.compressor = compressor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.maxPollIntervalMs = maxPollIntervalMs;
                //well within max.poll.interval.ms, librdkafka's default is 5 minutes
                this.busyPollInterval = maxPollIntervalMs.HasValue ? TimeSpan.FromMilliseconds(maxPollIntervalMs.Value / 4) : TimeSpan.FromSeconds(10);
                this.canceller = new CancellationTokenSource();
                if (resilient)
                    this.offsets = new();
            }

            public void Open(KafkaCommonHost commonHost)
            {
                if (IsOpen)
                    return;
                IsOpen = true;

                var producerConfig = new ProducerConfig();
                producerConfig.BootstrapServers = commonHost.Host;
                producerConfig.LingerMs = 0;
                //librdkafka keeps refreshing every topic it has produced to, with auto-create on that brings back the acknowledgement topic of a stopped producer
                producerConfig.AllowAutoCreateTopics = false;
                producerConfig.TopicMetadataPropagationMaxMs = 1000;
                producerConfig.ClientId = clientID;
                if (commonHost.UserName is not null && commonHost.Password is not null)
                {
                    producerConfig.SecurityProtocol = commonHost.UseTls ? SecurityProtocol.SaslSsl : SecurityProtocol.SaslPlaintext;
                    producerConfig.SaslMechanism = SaslMechanism.Plain;
                    producerConfig.SaslUsername = commonHost.UserName;
                    producerConfig.SaslPassword = commonHost.Password;
                }
                else if (commonHost.UseTls)
                {
                    producerConfig.SecurityProtocol = SecurityProtocol.Ssl;
                }
                ackProducerConfig = producerConfig;
                ackProducer = new AckProducer(new ProducerBuilder<string, byte[]>(producerConfig).Build());

                listening = Task.Run(() => ListeningThread(commonHost));
            }

            private async Task ListeningThread(KafkaCommonHost commonHost)
            {
                //the throttle is not disposed: handlers still running after the listener stops release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await KafkaCommon.EnsureTopic(commonHost, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = commonHost.Host;
                    consumerConfig.GroupId = topic;
                    consumerConfig.EnableAutoCommit = false;
                    if (maxPollIntervalMs.HasValue)
                    {
                        consumerConfig.MaxPollIntervalMs = maxPollIntervalMs;
                        //max.poll.interval.ms can't be under session.timeout.ms
                        consumerConfig.SessionTimeoutMs = Math.Min(maxPollIntervalMs.Value, 45000);
                    }
                    //commands in the topic are meant for this service, so a new group starts from the beginning instead of skipping commands sent before it first joined
                    consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
                    if (commonHost.UserName is not null && commonHost.Password is not null)
                    {
                        consumerConfig.SecurityProtocol = commonHost.UseTls ? SecurityProtocol.SaslSsl : SecurityProtocol.SaslPlaintext;
                        consumerConfig.SaslMechanism = SaslMechanism.Plain;
                        consumerConfig.SaslUsername = commonHost.UserName;
                        consumerConfig.SaslPassword = commonHost.Password;
                    }
                    else if (commonHost.UseTls)
                    {
                        consumerConfig.SecurityProtocol = SecurityProtocol.Ssl;
                    }

                    //librdkafka only reports a deleted topic as an error and keeps waiting, so it ends the consume and the retry creates the topic again
                    using var topicMissing = CancellationTokenSource.CreateLinkedTokenSource(canceller.Token);
                    var consumerBuilder = new ConsumerBuilder<string, byte[]>(consumerConfig).SetErrorHandler((_, error) =>
                    {
                        if (error.Code == ErrorCode.UnknownTopicOrPart || error.Code == ErrorCode.Local_UnknownPartition)
                            topicMissing.Cancel();
                    });
                    if (offsets is not null)
                    {
                        _ = consumerBuilder.SetPartitionsRevokedHandler(PartitionsRevoked);
                        _ = consumerBuilder.SetPartitionsLostHandler(PartitionsLost);
                    }
                    using (var consumer = consumerBuilder.Build())
                    {
                        consumer.Subscribe(topic);
                        try
                        {
                            for (; ; )
                            {
                                if (!throttle.Wait(0))
                                {
                                    //librdkafka drops a consumer from the group when it isn't polled within max.poll.interval.ms,
                                    //so while every handler is busy the partitions are paused and still polled
                                    consumer.Pause(consumer.Assignment);
                                    while (!await throttle.WaitAsync(busyPollInterval, topicMissing.Token))
                                    {
                                        var held = consumer.Consume(TimeSpan.Zero);
                                        if (held is not null)
                                        {
                                            //from a partition assigned since the pause, it's read again once resumed
                                            consumer.Seek(held.TopicPartitionOffset);
                                            consumer.Pause(consumer.Assignment);
                                        }
                                    }
                                    consumer.Resume(consumer.Assignment);
                                }

                                if (commandCounter is not null && !commandCounter.BeginReceive())
                                    break; //don't receive anymore, externally will be shutdown

                                ConsumeResult<string, byte[]> consumerResult;
                                PartitionOffsets? partitionOffsets = null;
                                try
                                {
                                    if (offsets is null)
                                    {
                                        consumerResult = consumer.Consume(topicMissing.Token);
                                        consumer.Commit(consumerResult);
                                    }
                                    else
                                    {
                                        //polled instead of waiting on the token so the commands handled in the meantime are committed
                                        for (; ; )
                                        {
                                            CommitHandled(consumer);
                                            var polled = consumer.Consume(resilientPollInterval);
                                            if (polled is not null)
                                            {
                                                consumerResult = polled;
                                                break;
                                            }
                                            topicMissing.Token.ThrowIfCancellationRequested();
                                        }
                                        lock (offsets)
                                        {
                                            if (!offsets.TryGetValue(consumerResult.TopicPartition, out partitionOffsets))
                                            {
                                                partitionOffsets = new PartitionOffsets();
                                                offsets.Add(consumerResult.TopicPartition, partitionOffsets);
                                            }
                                            _ = partitionOffsets.Handling.Add(consumerResult.Offset.Value);
                                            partitionOffsets.Next = consumerResult.Offset.Value + 1;
                                        }
                                    }
                                }
                                catch
                                {
                                    if (commandCounter is not null)
                                        commandCounter.CancelReceive(throttle);
                                    else
                                        _ = throttle.Release();
                                    throw;
                                }

                                var handleTask = Task.Run(() => HandleMessage(throttle, consumerResult, partitionOffsets));
                                _ = handling.Add(handleTask);
                                _ = handleTask.ContinueWith(removeHandling, handling, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                                //leaving the group hands the partition to another replica now instead of when this one exits
                                if (canceller.IsCancellationRequested || (commandCounter is not null && commandCounter.ReceiveLimitReached))
                                    break;
                            }
                        }
                        finally
                        {
                            if (offsets is not null)
                                await WaitForHandling(consumer);
                            //Close leaves the group right away, Unsubscribe and Dispose leave its member until the session times out
                            consumer.Close();
                            //a topic created again starts its offsets over
                            if (offsets is not null)
                            {
                                lock (offsets)
                                    offsets.Clear();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(topic, ex);
                        await KafkaCommon.ForgetTopic(commonHost, topic);
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            private void CommitHandled(IConsumer<string, byte[]> consumer)
            {
                List<TopicPartitionOffset>? commit = null;
                lock (offsets!)
                {
                    foreach (var pair in offsets)
                    {
                        var offset = pair.Value.Handling.Count > 0 ? pair.Value.Handling.Min : pair.Value.Next;
                        if (offset > pair.Value.Committed)
                            (commit ??= new()).Add(new TopicPartitionOffset(pair.Key, offset));
                    }
                }
                if (commit is null)
                    return;

                try
                {
                    consumer.Commit(commit);
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);
                    return;
                }

                lock (offsets)
                {
                    foreach (var committed in commit)
                    {
                        if (offsets.TryGetValue(committed.TopicPartition, out var partitionOffsets) && committed.Offset.Value > partitionOffsets.Committed)
                            partitionOffsets.Committed = committed.Offset.Value;
                    }
                }
            }

            //called from Consume and Close, the partitions stay assigned until it returns so it waits for their commands and commits them,
            //otherwise the consumer given them next would handle those commands again
            private void PartitionsRevoked(IConsumer<string, byte[]> consumer, List<TopicPartitionOffset> revoked)
            {
                var commit = new List<TopicPartitionOffset>();
                lock (offsets!)
                {
                    foreach (var partition in revoked)
                    {
                        if (!offsets.TryGetValue(partition.TopicPartition, out var partitionOffsets))
                            continue;
                        while (partitionOffsets.Handling.Count > 0)
                            _ = Monitor.Wait(offsets);
                        if (partitionOffsets.Next > partitionOffsets.Committed)
                            commit.Add(new TopicPartitionOffset(partition.TopicPartition, partitionOffsets.Next));
                        _ = offsets.Remove(partition.TopicPartition);
                    }
                }
                if (commit.Count == 0)
                    return;

                try
                {
                    consumer.Commit(commit);
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);
                }
            }

            //another consumer already has them and can't be committed for, the commands still being handled here may run there too
            private void PartitionsLost(IConsumer<string, byte[]> consumer, List<TopicPartitionOffset> lost)
            {
                lock (offsets!)
                {
                    foreach (var partition in lost)
                        _ = offsets.Remove(partition.TopicPartition);
                }
            }

            //before leaving the group the commands still being handled finish and are committed, the partitions are paused and still polled to stay in the group meanwhile
            private async Task WaitForHandling(IConsumer<string, byte[]> consumer)
            {
                try
                {
                    consumer.Pause(consumer.Assignment);
                    for (; ; )
                    {
                        var pending = handling.Where(x => !x.IsCompleted).ToArray();
                        if (pending.Length == 0)
                            break;
                        var all = Task.WhenAll(pending);
                        while (await Task.WhenAny(all, Task.Delay(busyPollInterval)) != all)
                        {
                            var held = consumer.Consume(TimeSpan.Zero);
                            if (held is not null)
                            {
                                consumer.Seek(held.TopicPartitionOffset);
                                consumer.Pause(consumer.Assignment);
                            }
                        }
                    }
                    CommitHandled(consumer);
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ConsumeResult<string, byte[]> consumerResult, PartitionOffsets? partitionOffsets)
            {
                object? result = null;
                Exception? error = null;
                var awaitResponse = consumerResult.Message.Key == KafkaCommon.MessageWithAckKey;

                var inHandlerContext = false;
                try
                {
                    if (consumerResult.Message.Key == KafkaCommon.MessageKey || consumerResult.Message.Key == KafkaCommon.MessageWithAckKey)
                    {
                        var body = consumerResult.Message.Value;
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body);
                        if (compressor is not null)
                            body = compressor.Decompress(body);

                        var message = serializer.Deserialize<KafkaMessage>(body);
                        if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                            throw new Exception("Invalid Message");

                        if (!commandTypes.TryGetValue(message.MessageType, out var commandType))
                            throw new Exception($"Unhandled Message Type {message.MessageType}");
                        var command = serializer.Deserialize(message.MessageData, commandType) as ICommand;
                        if (command is null)
                            throw new Exception("Invalid Message");

                        if (message.Claims is not null)
                        {
                            var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }

                        inHandlerContext = true;
                        if (message.HasResult)
                            result = await handlerWithResultAwaitAsync(command, message.Source, CancellationToken.None); //closing lets it finish instead of cancelling it
                        else if (awaitResponse)
                            await handlerAwaitAsync(command, message.Source, CancellationToken.None);
                        else
                            await handlerAsync(command, message.Source, default);
                        inHandlerContext = false;
                    }
                    else
                    {
                        log?.Error($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (!inHandlerContext)
                        log?.Error(topic, ex);
                }
                finally
                {
                    if (!awaitResponse)
                    {
                        if (partitionOffsets is not null)
                        {
                            lock (offsets!)
                            {
                                _ = partitionOffsets.Handling.Remove(consumerResult.Offset.Value);
                                Monitor.PulseAll(offsets);
                            }
                        }
                        if (commandCounter is not null)
                            commandCounter.CompleteReceive(throttle);
                        else
                            _ = throttle.Release();
                    }
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckTopicHeader));
                    var ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));

                    var acknowledgement = new Acknowledgement(serializer, result, error);
                    var body = serializer.SerializeBytes(acknowledgement);
                    if (compressor is not null)
                        body = compressor.Compress(body);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    AckProducer ackProducer;
                    for (; ; )
                    {
                        ackProducer = Volatile.Read(ref this.ackProducer)!;
                        _ = Interlocked.Increment(ref ackProducer.Using);
                        if (ackProducer == Volatile.Read(ref this.ackProducer))
                            break;
                        ackProducer.Release();
                    }
                    try
                    {
                        if (ackProducer.Topics.TryAdd(ackTopic, 0) && Interlocked.Increment(ref ackProducer.TopicCount) == maxAckTopicsPerProducer)
                        {
                            Volatile.Write(ref this.ackProducer, new AckProducer(new ProducerBuilder<string, byte[]>(ackProducerConfig!).Build()));
                            Volatile.Write(ref ackProducer.Retired, 1);
                            if (Volatile.Read(ref ackProducer.Using) == 0)
                                ackProducer.Dispose();
                        }

                        var ackMessage = new Message<string, byte[]>()
                        {
                            Key = ackKey!,
                            Value = body
                        };
                        for (var attempt = 1; ; attempt++)
                        {
                            try
                            {
                                _ = await ackProducer.Producer.ProduceAsync(ackTopic, ackMessage);
                                break;
                            }
                            catch (ProduceException<string, byte[]> ex) when (attempt < maxAckAttempts && (ex.Error.Code == ErrorCode.Local_UnknownTopic || ex.Error.Code == ErrorCode.UnknownTopicOrPart || ex.Error.Code == ErrorCode.Local_UnknownPartition))
                            {
                                if (attempt == 1)
                                    log?.Warn($"{nameof(KafkaConsumer)} failed to send an acknowledgement to {ackTopic}, trying again until its sender creates it again: {ex.Error.Code} {ex.Message}");
                                await Task.Delay(KafkaCommon.RetryDelay);
                                //the producer caches the topic as missing until its own handle refreshes the metadata
                                using (var producerAdmin = new DependentAdminClientBuilder(ackProducer.Producer.Handle).Build())
                                    _ = producerAdmin.GetMetadata(ackTopic, TimeSpan.FromSeconds(10));
                            }
                        }
                    }
                    finally
                    {
                        ackProducer.Release();
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
                finally
                {
                    if (partitionOffsets is not null)
                    {
                        lock (offsets!)
                        {
                            _ = partitionOffsets.Handling.Remove(consumerResult.Offset.Value);
                            Monitor.PulseAll(offsets);
                        }
                    }
                    if (commandCounter is not null)
                        commandCounter.CompleteReceive(throttle);
                    else
                        _ = throttle.Release();
                }
            }

            public void Close()
            {
                canceller.Cancel();
            }

            public void Dispose()
            {
                canceller.Cancel();

                //waits for the listener to stop so nothing new starts, then for the messages it already received, the ones that completed were already removed
                if (listening is not null)
                {
                    try
                    {
                        listening.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        log?.Error(topic, ex.InnerException ?? ex);
                    }
                }
                for (; ; )
                {
                    var pending = handling.Where(x => !x.IsCompleted).ToArray();
                    if (pending.Length == 0)
                        break;
                    try
                    {
                        Task.WaitAll(pending);
                    }
                    catch (AggregateException ex)
                    {
                        log?.Error(topic, ex.InnerException ?? ex);
                    }
                }

                canceller.Dispose();
                ackProducer?.Dispose();
            }

            public async ValueTask DisposeAsync()
            {
                canceller.Cancel();

                //waits for the listener to stop so nothing new starts, then for the messages it already received, the ones that completed were already removed
                if (listening is not null)
                {
                    try
                    {
                        await listening;
                    }
                    catch (Exception ex)
                    {
                        log?.Error(topic, ex);
                    }
                }
                for (; ; )
                {
                    var pending = handling.Where(x => !x.IsCompleted).ToArray();
                    if (pending.Length == 0)
                        break;
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        log?.Error(topic, ex);
                    }
                }

                canceller.Dispose();
                ackProducer?.Dispose();
            }
        }
    }
}
