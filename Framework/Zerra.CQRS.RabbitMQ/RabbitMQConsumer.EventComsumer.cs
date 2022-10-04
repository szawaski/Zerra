// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
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
        private class EventComsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            public readonly string topic;
            private readonly SymmetricConfig symmetricConfig;

            private IModel channel = null;
            private CancellationTokenSource canceller;

            public EventComsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(RabbitMQCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
            }

            public void Open(IConnection connection, Func<IEvent, Task> handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(connection, handlerAsync));
            }

            private async Task ListeningThread(IConnection connection, Func<IEvent, Task> handlerAsync)
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
                        var isEncrypted = e.BasicProperties.Headers != null && e.BasicProperties.Headers.Keys.Contains("Encryption") == true;

                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var properties = e.BasicProperties;
                        var acknowledgment = new Acknowledgement();

                        if (!isEncrypted && symmetricConfig != null)
                        {
                            acknowledgment.Success = false;
                            acknowledgment.ErrorMessage = "Encryption Required";
                        }
                        else
                        {
                            try
                            {
                                var body = e.Body;
                                if (isEncrypted)
                                {
                                    body = SymmetricEncryptor.Decrypt(symmetricConfig, e.Body);
                                }

                                var rabbitMessage = RabbitMQCommon.Deserialize<RabbitMQEventMessage>(body);

                                if (rabbitMessage.Claims != null)
                                {
                                    var claimsIdentity = new ClaimsIdentity(rabbitMessage.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                    System.Threading.Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                                }

                                await handlerAsync(rabbitMessage.Message);

                                stopwatch.Stop();

                                _ = Log.TraceAsync($"Received: {e.Exchange} {stopwatch.ElapsedMilliseconds}");

                                acknowledgment.Success = true;
                            }
                            catch (Exception ex)
                            {
                                stopwatch.Stop();

                                ex = ex.GetBaseException();

                                acknowledgment.Success = false;
                                acknowledgment.ErrorMessage = ex.Message;

                                _ = Log.TraceAsync($"Error: Received: {e.Exchange} {acknowledgment.ErrorMessage} {stopwatch.ElapsedMilliseconds}");

                                _ = Log.ErrorAsync(null, ex);
                            }
                        }
                    };

                    _ = this.channel.BasicConsume(queueName, false, consumer);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);

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

                canceller.Dispose();
                canceller = null;

                if (channel != null)
                {
                    channel.Close();
                    channel.Dispose();
                    channel = null;
                }

                IsOpen = false;
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
                GC.SuppressFinalize(this);
            }
        }
    }
}
