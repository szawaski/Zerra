// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
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

            private IModel? channel = null;
            private SemaphoreSlim? throttle = null;

            public EventConsumer(int maxConcurrent, string topic, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = maxConcurrent;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
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

                throttle?.Dispose();
                throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

                try
                {
                    if (this.channel is not null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.BasicQos(0, (ushort)maxConcurrent, false);
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Fanout);

                    var queue = this.channel.QueueDeclare(String.Empty, false, true, true);
                    this.channel.QueueBind(queue.QueueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        await throttle.WaitAsync(canceller.Token);

                        this.channel.BasicAck(e.DeliveryTag, false);

                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQMessage? message;
                            if (encryptor is not null)
                                message = serializer.Deserialize<RabbitMQMessage>(encryptor.Decrypt(e.Body.Span));
                            else
                                message = serializer.Deserialize<RabbitMQMessage>(e.Body.Span);

                            if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                                throw new Exception("Invalid Message");

                            var @event = serializer.Deserialize(message.MessageData, message.MessageType) as IEvent;
                            if (@event is null)
                                throw new Exception("Invalid Message");

                            if (message.Claims is not null)
                            {
                                var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            inHandlerContext = true;
                            await handlerAsync(@event, message.Source, false);
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
                    log?.Error(topic, ex);

                    if (!canceller.IsCancellationRequested)
                    {
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

                throttle?.Dispose();

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
