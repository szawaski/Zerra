// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMessage
{
    public partial class RabbitServer
    {
        private class EventReceiverExchange : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get { return this.channel != null; } }

            public readonly string exchange;
            private readonly SymmetricKey encryptionKey;

            private IModel channel = null;
            private AsyncEventingBasicConsumer consumer = null;

            public EventReceiverExchange(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.exchange = type.GetNiceName();
                this.encryptionKey = encryptionKey;
            }

            public void Open(IConnection connection, Func<IEvent, Task> handlerAsync)
            {
                try
                {
                    if (this.channel != null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.ExchangeDeclare(this.exchange, "fanout");

                    var queueName = this.channel.QueueDeclare().QueueName;
                    this.channel.QueueBind(queueName, this.exchange, String.Empty);

                    this.consumer = new AsyncEventingBasicConsumer(this.channel);

                    this.consumer.Received += async (sender, e) =>
                    {
                        bool isEncrypted = e.BasicProperties.Headers != null && e.BasicProperties.Headers.Keys.Contains("Encryption") == true;

                        var stopwatch = new Stopwatch();
                        stopwatch.Start();

                        var properties = e.BasicProperties;
                        var acknowledgment = new Acknowledgement();

                        if (!isEncrypted && encryptionKey != null)
                        {
                            acknowledgment.Success = false;
                            acknowledgment.ErrorMessage = "Encryption Required";
                        }
                        else
                        {
                            try
                            {
                                byte[] body = e.Body;
                                if (isEncrypted)
                                {
                                    body = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, e.Body, true);
                                }

                                var rabbitMessage = RabbitCommon.Deserialize<RabbitEventMessage>(body);

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


                    this.channel.BasicConsume(queueName, false, this.consumer);
                }
                catch (Exception ex)
                {
                    Log.ErrorAsync(null, ex);
                    throw;
                }
            }

            public void Dispose()
            {
                if (this.channel != null)
                {
                    this.consumer = null;
                    this.channel.Close();
                    this.channel.Dispose();
                    this.channel = null;
                }
            }
        }
    }
}
