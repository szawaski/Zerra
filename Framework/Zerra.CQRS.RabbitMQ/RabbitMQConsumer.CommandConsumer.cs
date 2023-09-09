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
        private sealed class CommandConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricConfig symmetricConfig;

            private IModel channel = null;
            private readonly CancellationTokenSource canceller;

            public CommandConsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(RabbitMQCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(IConnection connection, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(connection, handlerAsync, handlerAwaitAsync);
            }

            private async Task ListeningThread(IConnection connection, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
            retry:

                try
                {
                    if (this.channel != null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Direct);

                    var queueName = this.channel.QueueDeclare().QueueName;
                    this.channel.QueueBind(queueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        var awaitResponse = !String.IsNullOrWhiteSpace(e.BasicProperties.ReplyTo);

                        Acknowledgement acknowledgment;
                        if (awaitResponse)
                            acknowledgment = new Acknowledgement();
                        else
                            acknowledgment = null;

                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQCommandMessage message;
                            if (symmetricConfig != null)
                                message = RabbitMQCommon.Deserialize<RabbitMQCommandMessage>(SymmetricEncryptor.Decrypt(symmetricConfig, e.Body.Span));
                            else
                                message = RabbitMQCommon.Deserialize<RabbitMQCommandMessage>(e.Body.Span);

                            if (message.Claims != null)
                            {
                                var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            inHandlerContext = true;
                            if (awaitResponse)
                                await handlerAwaitAsync(message.Message, message.Source, false);
                            else
                                await handlerAsync(message.Message, message.Source, false);
                            inHandlerContext = false;

                            if (acknowledgment != null)
                            {
                                acknowledgment.Success = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!inHandlerContext)
                                _ = Log.ErrorAsync(topic, ex);

                            if (acknowledgment != null)
                            {
                                acknowledgment.Success = false;
                                acknowledgment.ErrorMessage = ex.Message;
                            }
                        }

                        if (acknowledgment != null)
                        {
                            try
                            {
                                var replyProperties = this.channel.CreateBasicProperties();
                                replyProperties.CorrelationId = e.BasicProperties.CorrelationId;

                                var acknowledgmentBody = RabbitMQCommon.Serialize(acknowledgment);
                                if (symmetricConfig != null)
                                    acknowledgmentBody = SymmetricEncryptor.Encrypt(symmetricConfig, acknowledgmentBody);

                                this.channel.BasicPublish(String.Empty, e.BasicProperties.ReplyTo, replyProperties, acknowledgmentBody);
                            }
                            catch (Exception ex)
                            {
                                _ = Log.ErrorAsync(topic, ex);
                            }
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
