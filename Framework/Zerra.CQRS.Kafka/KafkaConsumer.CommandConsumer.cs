// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Zerra.Collections;
using System.Security.Claims;
using System.Text;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        private sealed class CommandConsumer : IDisposable, IAsyncDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter commandCounter;
            private readonly string topic;
            private readonly string clientID;
            private readonly Zerra.Serialization.ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILogger? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);
            private Task? listening;
            private IProducer<string, byte[]>? ackProducer;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(KafkaConsumer)} truncated the command topic to {KafkaCommon.TopicMaxLength} characters: {this.topic}. Another topic truncating to the same name would be consumed as this one.");
                this.clientID = Environment.MachineName;
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, string? userName, string? password)
            {
                if (IsOpen)
                    return;
                IsOpen = true;

                //one producer sends every acknowledgement, it connects on first use
                var producerConfig = new ProducerConfig();
                producerConfig.BootstrapServers = host;
                producerConfig.ClientId = clientID;
                if (userName is not null && password is not null)
                {
                    producerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                    producerConfig.SaslMechanism = SaslMechanism.Plain;
                    producerConfig.SaslUsername = userName;
                    producerConfig.SaslPassword = password;
                }
                ackProducer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

                listening = Task.Run(() => ListeningThread(host, userName, password));
            }

            private async Task ListeningThread(string host, string? userName, string? password)
            {
                //the throttle is not disposed: handlers still running after the listener stops release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await KafkaCommon.EnsureTopic(host, userName, password, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = host;
                    consumerConfig.GroupId = topic;
                    consumerConfig.EnableAutoCommit = false;
                    //commands in the topic are meant for this service, so a new group starts from the beginning instead of skipping commands sent before it first joined
                    consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
                    if (userName is not null && password is not null)
                    {
                        consumerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                        consumerConfig.SaslMechanism = SaslMechanism.Plain;
                        consumerConfig.SaslUsername = userName;
                        consumerConfig.SaslPassword = password;
                    }

                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                    {
                        consumer.Subscribe(topic);
                        try
                        {
                            for (; ; )
                            {
                                await throttle.WaitAsync(canceller.Token);

                                if (!commandCounter.BeginReceive())
                                    continue; //don't receive anymore, externally will be shutdown, fill throttle

                                ConsumeResult<string, byte[]> consumerResult;
                                try
                                {
                                    consumerResult = consumer.Consume(canceller.Token);
                                    consumer.Commit(consumerResult);
                                }
                                catch
                                {
                                    commandCounter.CancelReceive(throttle);
                                    throw;
                                }

                                var handleTask = Task.Run(() => HandleMessage(throttle, consumerResult));
                                _ = handling.Add(handleTask);
                                _ = handleTask.ContinueWith(removeHandling, handling, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                                if (canceller.IsCancellationRequested)
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
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(topic, ex);
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
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body);

                        var message = serializer.Deserialize<KafkaMessage>(body);
                        if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                            throw new Exception("Invalid Message");

                        var command = serializer.Deserialize(message.MessageData, TypeFinder.GetTypeFromName(message.MessageType)) as ICommand;
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
                        commandCounter.CompleteReceive(throttle);
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckTopicHeader));
                    var ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));

                    var acknowledgement = new Acknowledgement(serializer, result, error);
                    var body = serializer.SerializeBytes(acknowledgement);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    _ = await ackProducer!.ProduceAsync(ackTopic, new Message<string, byte[]>()
                    {
                        Key = ackKey!,
                        Value = body
                    });
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
                finally
                {
                    commandCounter.CompleteReceive(throttle);
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
