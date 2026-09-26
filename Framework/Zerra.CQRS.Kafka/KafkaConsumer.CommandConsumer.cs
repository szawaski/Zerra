// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        public sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter commandCounter;
            private readonly string topic;
            private readonly string clientID;
            private readonly SymmetricConfig? symmetricConfig;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
            private ProducerConfig? ackProducerConfig;
            private AckProducer? ackProducer;

            //librdkafka keeps every topic a producer has sent to for the producer's life, so after this many acknowledgement topics it's replaced
            private const int maxAckTopicsPerProducer = 1000;
            //well within max.poll.interval.ms, librdkafka's default is 5 minutes
            private static readonly TimeSpan busyPollInterval = TimeSpan.FromSeconds(10);

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

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength);
                this.clientID = Environment.MachineName;
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, string? userName, string? password) => Open(KafkaCommon.GetHost(host, userName, password));

            internal void Open(KafkaCommonHost commonHost)
            {
                if (IsOpen)
                    return;
                IsOpen = true;

                var producerConfig = new ProducerConfig();
                producerConfig.BootstrapServers = commonHost.Host;
                producerConfig.LingerMs = 0;
                //librdkafka keeps refreshing every topic it has produced to, with auto-create on that brings back the acknowledgement topic of a stopped producer
                producerConfig.AllowAutoCreateTopics = false;
                producerConfig.ClientId = clientID;
                if (commonHost.UserName is not null && commonHost.Password is not null)
                {
                    producerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                    producerConfig.SaslMechanism = SaslMechanism.Plain;
                    producerConfig.SaslUsername = commonHost.UserName;
                    producerConfig.SaslPassword = commonHost.Password;
                }
                ackProducerConfig = producerConfig;
                ackProducer = new AckProducer(new ProducerBuilder<string, byte[]>(producerConfig).Build());

                _ = Task.Run(() => ListeningThread(commonHost));
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
                    //commands in the topic are meant for this service, so a new group starts from the beginning instead of skipping commands sent before it first joined
                    consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
                    if (commonHost.UserName is not null && commonHost.Password is not null)
                    {
                        consumerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                        consumerConfig.SaslMechanism = SaslMechanism.Plain;
                        consumerConfig.SaslUsername = commonHost.UserName;
                        consumerConfig.SaslPassword = commonHost.Password;
                    }

                    //librdkafka only reports a deleted topic as an error and keeps waiting, so it ends the consume and the retry creates the topic again
                    using var topicMissing = CancellationTokenSource.CreateLinkedTokenSource(canceller.Token);
                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).SetErrorHandler((_, error) =>
                    {
                        if (error.Code == ErrorCode.UnknownTopicOrPart || error.Code == ErrorCode.Local_UnknownPartition)
                            topicMissing.Cancel();
                    }).Build())
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

                                if (!commandCounter.BeginReceive())
                                    break; //don't receive anymore, externally will be shutdown

                                ConsumeResult<string, byte[]> consumerResult;
                                try
                                {
                                    consumerResult = consumer.Consume(topicMissing.Token);
                                    consumer.Commit(consumerResult);
                                }
                                catch
                                {
                                    //the retry keeps this throttle, give back the permit taken for the message that wasn't received
                                    //an uncommitted message is consumed again from the last committed offset after reconnecting
                                    commandCounter.CancelReceive(throttle);
                                    throw;
                                }

                                _ = Task.Run(() => HandleMessage(throttle, consumerResult));

                                //leaving the group hands the partition to another replica now instead of when this one exits
                                if (canceller.IsCancellationRequested || commandCounter.ReceiveLimitReached)
                                    break;
                            }
                        }
                        finally
                        {
                            //Close leaves the group right away, Unsubscribe and Dispose leave its member until the session times out
                            consumer.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //closing cancels the consume, that isn't an error
                    if (!canceller.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(topic, ex);
                        await KafkaCommon.ForgetTopic(commonHost, topic);
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ConsumeResult<string, byte[]> consumerResult)
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
                        if (symmetricConfig is not null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                        var message = KafkaCommon.Deserialize<KafkaMessage>(body);
                        if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                            throw new Exception("Invalid Message");

                        var command = KafkaCommon.Deserialize(message.MessageData, message.MessageType) as ICommand;
                        if (command is null)
                            throw new Exception("Invalid Message");

                        if (message.Claims is not null)
                        {
                            var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }

                        inHandlerContext = true;
                        if (message.HasResult)
                            result = await handlerWithResultAwaitAsync(command, message.Source, false, canceller.Token);
                        else if (awaitResponse)
                            await handlerAwaitAsync(command, message.Source, false, canceller.Token);
                        else
                            await handlerAsync(command, message.Source, false, default);
                        inHandlerContext = false;
                    }
                    else
                    {
                        _ = Log.ErrorAsync($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(topic, ex);
                }
                finally
                {
                    if (!awaitResponse)
                        commandCounter.CompleteReceive(throttle);
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckTopicHeader));
                    var ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));

                    var acknowledgement = new Acknowledgement(result, error);
                    var body = KafkaCommon.Serialize(acknowledgement);
                    if (symmetricConfig is not null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

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

                        _ = await ackProducer.Producer.ProduceAsync(ackTopic, new Message<string, byte[]>()
                        {
                            Key = ackKey!,
                            Value = body
                        });
                    }
                    finally
                    {
                        ackProducer.Release();
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                }
                finally
                {
                    commandCounter.CompleteReceive(throttle);
                }
            }

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();
                ackProducer?.Dispose();
            }
        }
    }
}
