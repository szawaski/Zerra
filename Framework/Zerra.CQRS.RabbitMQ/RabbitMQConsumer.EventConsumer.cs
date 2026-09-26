// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed partial class RabbitMQConsumer
    {
        private sealed class EventConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly string topic;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILogger? log;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;
            //null for PerReplica so the broker names an exclusive queue for this replica alone, a queue named for the service for PerService so its replicas compete for the events
            private readonly string? queue;

            private IModel? channel = null;
            private readonly SemaphoreSlim throttle;

            public EventConsumer(int maxConcurrent, string topic, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, string serviceName, EventConsumerMode eventConsumerMode, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = maxConcurrent;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(RabbitMQConsumer)} truncated the event exchange to {RabbitMQCommon.TopicMaxLength} characters: {this.topic}. Another exchange truncating to the same name would be consumed as this one.");
                if (eventConsumerMode == EventConsumerMode.PerService)
                {
                    this.queue = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", this.topic, serviceName, out truncated);
                    if (truncated)
                        log?.Warn($"{nameof(RabbitMQConsumer)} truncated the {EventConsumerMode.PerService} queue to {RabbitMQCommon.TopicMaxLength} characters: {this.queue}. Another service truncating to the same queue would compete with this one for the events.");
                }
                else
                {
                    this.queue = null;
                }
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
                //one throttle for the life of the consumer, reconnecting keeps the permits held by events still being handled
                this.throttle = new SemaphoreSlim(this.maxConcurrent, this.maxConcurrent);
            }

            public void Open(IConnection connection)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(connection));
            }

            private async Task ListeningThread(IConnection connection)
            {
            retry:

                try
                {
                    if (this.channel is not null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.BasicQos(0, (ushort)maxConcurrent, false);
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Fanout);

                    //an exclusive server named queue reaches this replica alone, a queue named for the service is shared by its replicas so they compete.
                    //the shared one is durable because a broker refuses a transient queue that isn't exclusive, it only makes the queue survive a restart
                    //and the events are still transient; auto delete still takes the queue with the last replica to disconnect
                    var queue = this.queue is null
                        ? this.channel.QueueDeclare(String.Empty, false, true, true)
                        : this.channel.QueueDeclare(this.queue, true, false, true);
                    this.channel.QueueBind(queue.QueueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        await throttle.WaitAsync(canceller.Token);

                        try
                        {
                            //delivery tags belong to the channel that delivered, a reconnect may have replaced this.channel while waiting on the throttle
                            consumer.Model.BasicAck(e.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            //the channel closed, the broker redelivers the unacknowledged message after reconnecting
                            log?.Error(topic, ex);
                            _ = throttle.Release();
                            return;
                        }

                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQMessage? message;
                            if (encryptor is not null)
#if NETSTANDARD2_0
                                message = serializer.Deserialize<RabbitMQMessage>(encryptor.Decrypt(e.Body.ToArray()));
#else
                                message = serializer.Deserialize<RabbitMQMessage>(encryptor.Decrypt(e.Body.Span));
#endif
                            else
                                message = serializer.Deserialize<RabbitMQMessage>(e.Body.Span);

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
                        catch (Exception ex)
                        {
                            if (!inHandlerContext)
                                log?.Error(topic, ex);
                        }
                        finally
                        {
                            _ = throttle.Release();
                        }
                    };

                    _ = this.channel.BasicConsume(queue.QueueName, false, consumer);
                }
                catch (Exception ex)
                {
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(topic, ex);

                        if (channel is not null)
                        {
                            channel.Close();
                            channel.Dispose();
                            channel = null;
                        }
                        await Task.Delay(RabbitMQCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();

                //the throttle is not disposed: handlers still running after Dispose release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished

                if (channel is not null)
                {
                    channel.Close();
                    channel.Dispose();
                    channel = null;
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
