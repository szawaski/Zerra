// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.RabbitMessage
{
    public class RabbitMessageClient : ICommandClient, IEventClient, IDisposable
    {
        private readonly string hostName;
        private readonly SymmetricKey encryptionKey;
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.RijndaelManaged;
        private IConnection connection = null;

        public string ServiceUrl => hostName;

        public RabbitMessageClient(string hostName, SymmetricKey encryptionKey)
        {
            this.hostName = hostName;
            this.encryptionKey = encryptionKey;
            try
            {
                var factory = new ConnectionFactory() { HostName = hostName };
                this.connection = factory.CreateConnection();
                _ = Log.TraceAsync($"{nameof(RabbitMessageClient)} Started For {this.hostName}");
            }
            catch (Exception ex)
            {
                Log.ErrorAsync(null, ex);
                throw;
            }
        }

        Task ICommandClient.DispatchAsync(ICommand command)
        {
            return SendAsync(command, false);
        }
        Task ICommandClient.DispatchAsyncAwait(ICommand command)
        {
            return SendAsync(command, true);
        }

        Task IEventClient.DispatchAsync(IEvent @event)
        {
            return SendAsync(@event);
        }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                if (connection.IsOpen == false)
                {
                    lock (this)
                    {
                        if (connection.IsOpen == false)
                        {
                            var factory = new ConnectionFactory() { HostName = hostName };
                            this.connection = factory.CreateConnection();
                            _ = Log.TraceAsync($"Sender Reconnected: {hostName}");
                        }
                    }
                }

                var channel = connection.CreateModel();

                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var rabbitMessage = new RabbitCommandMessage()
                {
                    Message = command,
                    Claims = claims
                };

                byte[] body = RabbitMessageCommon.Serialize(rabbitMessage);
                if (encryptionKey != null)
                {
                    body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body, true);
                }

                string exchange = command.GetType().GetNiceName();


                var properties = channel.CreateBasicProperties();

                EventingBasicConsumer consumer = null;
                string consumerTag = null;
                string correlationId = null;
                if (requireAcknowledgement)
                {
                    string replyQueueName = channel.QueueDeclare().QueueName;
                    consumer = new EventingBasicConsumer(channel);
                    consumerTag = channel.BasicConsume(replyQueueName, false, consumer);

                    correlationId = Guid.NewGuid().ToString();
                    properties.ReplyTo = replyQueueName;
                    properties.CorrelationId = correlationId;
                }

                if (encryptionKey != null)
                {
                    Dictionary<string, object> messageHeaders = new Dictionary<string, object>();
                    messageHeaders.Add("Encryption", true);
                    properties.Headers = messageHeaders;
                }

                channel.BasicPublish(exchange, String.Empty, properties, body);

                _ = Log.TraceAsync($"Sent{(requireAcknowledgement ? " Await" : null)}: {exchange}");

                if (requireAcknowledgement)
                {
                    Exception exception = null;
                    var syncEvent = new SemaphoreSlim(0, 1);

                    consumer.Received += (sender, e) =>
                    {
                        try
                        {
                            if (e.BasicProperties.CorrelationId != correlationId)
                                throw new Exception("Response keys should be single and unique");

                            channel.BasicCancel(consumerTag);

                            byte[] acknowledgementBody = e.Body;
                            if (encryptionKey != null)
                            {
                                acknowledgementBody = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, acknowledgementBody, true);
                            }
                            var affirmation = RabbitMessageCommon.Deserialize<Acknowledgement>(acknowledgementBody);

                            stopwatch.Stop();

                            if (!affirmation.Success)
                                _ = Log.TraceAsync($"Await Failed: {exchange}: {affirmation.ErrorMessage} {stopwatch.ElapsedMilliseconds}");
                            else
                                _ = Log.TraceAsync($"Await Success: {exchange} {stopwatch.ElapsedMilliseconds}");

                            if (!affirmation.Success)
                            {
                                exception = new AcknowledgementException(affirmation, exchange);
                            }
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        finally
                        {
                            syncEvent.Release();
                        }
                    };

                    await syncEvent.WaitAsync();
                    syncEvent.Dispose();

                    if (exception != null)
                    {
                        throw exception;
                    }
                }

                channel.Close();
                channel.Dispose();
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(null, ex);
                throw;
            }
        }

        private Task SendAsync(IEvent @event)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                if (connection.IsOpen == false)
                {
                    lock (this)
                    {
                        if (connection.IsOpen == false)
                        {
                            var factory = new ConnectionFactory() { HostName = hostName };
                            this.connection = factory.CreateConnection();
                            _ = Log.TraceAsync($"Sender Reconnected: {hostName}");
                        }
                    }
                }

                var channel = connection.CreateModel();

                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var rabbitMessage = new RabbitEventMessage()
                {
                    Message = @event,
                    Claims = claims
                };

                byte[] body = RabbitMessageCommon.Serialize(rabbitMessage);
                if (encryptionKey != null)
                {
                    body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body, true);
                }

                string exchange = @event.GetType().GetNiceName();

                channel.ExchangeDeclare(exchange, "fanout");

                var properties = channel.CreateBasicProperties();

                if (encryptionKey != null)
                {
                    Dictionary<string, object> messageHeaders = new Dictionary<string, object>();
                    messageHeaders.Add("Encryption", true);
                    properties.Headers = messageHeaders;
                }

                channel.BasicPublish(exchange, String.Empty, properties, body);

                _ = Log.TraceAsync($"Sent: {exchange}");

                channel.Close();
                channel.Dispose();

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(null, ex);
                throw;
            }
        }

        public void Dispose()
        {
            this.connection.Close();
            this.connection.Dispose();
        }
    }
}
