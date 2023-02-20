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
    public partial class RabbitMQConsumer
    {
        private class CommandComsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricConfig symmetricConfig;

            private IModel channel = null;
            private CancellationTokenSource canceller;

            public CommandComsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(RabbitMQCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
            }

            public void Open(IConnection connection, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(connection, handlerAsync, handlerAwaitAsync));
            }

            private async Task ListeningThread(IConnection connection, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                try
                {
                    if (this.channel != null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.ExchangeDeclare(this.topic, "fanout");

                    var queueName = this.channel.QueueDeclare().QueueName;
                    this.channel.QueueBind(queueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        _ = Log.TraceAsync($"Received: {topic}");

                        var properties = e.BasicProperties;
                        var acknowledgment = new Acknowledgement();

                        var awaitResponse = !String.IsNullOrWhiteSpace(properties.ReplyTo);

                        try
                        {
                            var body = e.Body;
                            if (symmetricConfig != null)
                                body = SymmetricEncryptor.Decrypt(symmetricConfig, e.Body);

                            var rabbitMessage = RabbitMQCommon.Deserialize<RabbitMQCommandMessage>(body);

                            if (rabbitMessage.Claims != null)
                            {
                                var claimsIdentity = new ClaimsIdentity(rabbitMessage.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            if (awaitResponse)
                                await handlerAwaitAsync(rabbitMessage.Message);
                            else
                                await handlerAsync(rabbitMessage.Message);

                            acknowledgment.Success = true;
                        }
                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync($"Error: {topic}", ex);

                            acknowledgment.Success = false;
                            acknowledgment.ErrorMessage = ex.Message;                          
                        }

                        if (awaitResponse)
                        {
                            try
                            {
                                var replyProperties = this.channel.CreateBasicProperties();
                                replyProperties.CorrelationId = properties.CorrelationId;

                                var acknowledgmentBody = RabbitMQCommon.Serialize(acknowledgment);
                                if (symmetricConfig != null)
                                    acknowledgmentBody = SymmetricEncryptor.Encrypt(symmetricConfig, acknowledgmentBody);

                                this.channel.BasicPublish(String.Empty, properties.ReplyTo, replyProperties, acknowledgmentBody);
                            }
                            catch (Exception ex)
                            {
                                _ = Log.ErrorAsync($"Error: {topic}", ex);
                            }
                        }
                    };

                    _ = this.channel.BasicConsume(queueName, false, consumer);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync($"Error: {topic}", ex);

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
                if (canceller != null)
                    canceller.Cancel();

                if (channel != null)
                {
                    channel.Close();
                    channel.Dispose();
                    channel = null;
                }

                IsOpen = false;
                GC.SuppressFinalize(this);
            }
        }
    }
}
