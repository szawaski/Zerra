// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Zerra.Collections;
using System.Security.Claims;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        private sealed class EventConsumer : IDisposable, IAsyncDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly string topic;
            private readonly Zerra.Serialization.ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILogger? log;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);
            private Task? listening;
            //PerReplica gets a group of its own so this replica receives every event, it's kept across retries and deleted when the consumer stops
            //PerService gets a group named for the service so its replicas compete for the events, it's shared so it's never deleted
            private readonly string groupId;
            private readonly bool deleteGroupOnStop;

            public EventConsumer(int maxConcurrent, string topic, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, string serviceName, EventConsumerMode eventConsumerMode, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = maxConcurrent;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(KafkaConsumer)} truncated the event topic to {KafkaCommon.TopicMaxLength} characters: {this.topic}. Another topic truncating to the same name would be consumed as this one.");
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
                if (eventConsumerMode == EventConsumerMode.PerService)
                {
                    this.groupId = StringExtensions.Join(KafkaCommon.GroupMaxLength, "_", this.topic, serviceName, out truncated);
                    if (truncated)
                        log?.Warn($"{nameof(KafkaConsumer)} truncated the {EventConsumerMode.PerService} consumer group to {KafkaCommon.GroupMaxLength} characters: {this.groupId}. Another service truncating to the same group would compete with this one for the events.");
                    this.deleteGroupOnStop = false;
                }
                else
                {
                    this.groupId = Guid.NewGuid().ToString("N");
                    this.deleteGroupOnStop = true;
                }
            }

            public void Open(string host, string? userName, string? password)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                listening = Task.Run(() => ListeningThread(host, userName, password, handlerAsync));
            }

            public async Task ListeningThread(string host, string? userName, string? password, HandleRemoteEventDispatch handlerAsync)
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
                    consumerConfig.GroupId = groupId;
                    consumerConfig.EnableAutoCommit = false;
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

                                ConsumeResult<string, byte[]> consumerResult;
                                try
                                {
                                    consumerResult = consumer.Consume(canceller.Token);
                                    consumer.Commit(consumerResult);
                                }
                                catch
                                {
                                    _ = throttle.Release();
                                    throw;
                                }

                                var handleTask = Task.Run(() => HandleMessage(throttle, host, consumerResult, handlerAsync));
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

                //only reached once the consumer is stopping and has left the group, a PerService group belongs to the other replicas too so it stays
                if (deleteGroupOnStop)
                {
                    try
                    {
                        await KafkaCommon.DeleteConsumerGroup(host, userName, password, groupId);
                    }
                    catch (Exception ex)
                    {
                        log?.Error(topic, ex);
                    }
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, string host, ConsumeResult<string, byte[]> consumerResult, HandleRemoteEventDispatch handlerAsync)
            {
                var inHandlerContext = false;
                try
                {
                    if (consumerResult.Message.Key == KafkaCommon.MessageKey)
                    {
                        var body = consumerResult.Message.Value;
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body);

                        var message = serializer.Deserialize<KafkaMessage>(body);

                        if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                            throw new Exception("Invalid Message");

                        var @event = serializer.Deserialize(message.MessageData, TypeFinder.GetTypeFromName(message.MessageType)) as IEvent;
                        if (@event is null)
                            throw new Exception("Invalid Message");

                        if (message.Claims is not null)
                        {
                            var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }

                        inHandlerContext = true;
                        await handlerAsync(@event, message.Source);
                        inHandlerContext = false;
                    }
                    else
                    {
                        log?.Error($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    //the bus logs handler errors, anything before the handler such as a message that can't be read is logged here
                    if (!inHandlerContext)
                        log?.Error(topic, ex);
                }
                finally
                {
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
            }
        }
    }
}
