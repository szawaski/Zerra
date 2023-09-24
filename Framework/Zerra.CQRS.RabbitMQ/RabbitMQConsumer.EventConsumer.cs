// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed partial class RabbitMQConsumer
    {
        private sealed class EventConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricConfig symmetricConfig;

            private IModel channel = null;
            private readonly CancellationTokenSource canceller;

            public EventConsumer(string topic, SymmetricConfig symmetricConfig, string environment)
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{topic}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(IConnection connection, HandleRemoteEventDispatch handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(connection, handlerAsync);
            }

            private async Task ListeningThread(IConnection connection, HandleRemoteEventDispatch handlerAsync)
            {
            retry:

                try
                {
                    if (this.channel != null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Fanout);

                    var queueName = this.channel.QueueDeclare().QueueName;
                    this.channel.QueueBind(queueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQEventMessage message;
                            if (symmetricConfig != null)
                                message = RabbitMQCommon.Deserialize<RabbitMQEventMessage>(SymmetricEncryptor.Decrypt(symmetricConfig, e.Body.Span));
                            else
                                message = RabbitMQCommon.Deserialize<RabbitMQEventMessage>(e.Body.Span);

                            if (message.Claims != null)
                            {
                                var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            inHandlerContext = true;
                            await handlerAsync(message.Message, message.Source, false);
                            inHandlerContext = false;
                        }
                        catch (Exception ex)
                        {
                            if (!inHandlerContext)
                                _ = Log.ErrorAsync(topic, ex);
                        }
                    };

                    _ = this.channel.BasicConsume(queueName, true, consumer);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(topic, ex);

                    if (!canceller.IsCancellationRequested)
                    {
                        if (channel != null)
                        {
                            channel.Close();
                            channel.Dispose();
                            channel = null;
                        }
                        await Task.Delay(retryDelay);
                        goto retry;
                    }
                }
            }

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();

                if (channel != null)
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
