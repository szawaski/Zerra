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

namespace Zerra.CQRS.RabbitMQ
{
    public class RabbitMQProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;
        private IConnection connection = null;

        public string ConnectionString => host;

        public RabbitMQProducer(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;
            try
            {
                var factory = new ConnectionFactory() { HostName = host };
                this.connection = factory.CreateConnection();
                _ = Log.TraceAsync($"{nameof(RabbitMQProducer)} Started For {this.host}");
            }
            catch (Exception ex)
            {
                Log.ErrorAsync(null, ex);
                throw;
            }
        }

        Task ICommandProducer.DispatchAsync(ICommand command) { return SendAsync(command, false); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command) { return SendAsync(command, true); }
        Task IEventProducer.DispatchAsync(IEvent @event) { return SendAsync(@event); }

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
                            var factory = new ConnectionFactory() { HostName = host };
                            this.connection = factory.CreateConnection();
                            _ = Log.TraceAsync($"Sender Reconnected: {host}");
                        }
                    }
                }

                var channel = connection.CreateModel();

                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var rabbitMessage = new RabbitMQCommandMessage()
                {
                    Message = command,
                    Claims = claims
                };

                var body = RabbitMQCommon.Serialize(rabbitMessage);
                if (encryptionKey != null)
                    body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

                var topic = command.GetType().GetNiceName();

                var properties = channel.CreateBasicProperties();

                EventingBasicConsumer consumer = null;
                string consumerTag = null;
                string correlationId = null;
                if (requireAcknowledgement)
                {
                    var replyQueueName = channel.QueueDeclare().QueueName;
                    consumer = new EventingBasicConsumer(channel);
                    consumerTag = channel.BasicConsume(replyQueueName, false, consumer);

                    correlationId = Guid.NewGuid().ToString();
                    properties.ReplyTo = replyQueueName;
                    properties.CorrelationId = correlationId;
                }

                if (encryptionKey != null)
                {
                    var messageHeaders = new Dictionary<string, object>();
                    messageHeaders.Add("Encryption", true);
                    properties.Headers = messageHeaders;
                }

                channel.BasicPublish(topic, String.Empty, properties, body);

                _ = Log.TraceAsync($"Sent{(requireAcknowledgement ? " Await" : null)}: {topic}");

                if (requireAcknowledgement)
                {
                    Exception exception = null;
                    var waiter = new SemaphoreSlim(0, 1);

                    consumer.Received += (sender, e) =>
                    {
                        try
                        {
                            if (e.BasicProperties.CorrelationId != correlationId)
                                throw new Exception("ACK response CorrelationIds should be single and unique");

                            channel.BasicCancel(consumerTag);

                            var acknowledgementBody = e.Body;
                            if (encryptionKey != null)
                                acknowledgementBody = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, acknowledgementBody);
                            
                            var affirmation = RabbitMQCommon.Deserialize<Acknowledgement>(acknowledgementBody);

                            stopwatch.Stop();

                            if (!affirmation.Success)
                                _ = Log.TraceAsync($"Await Failed: {topic}: {affirmation.ErrorMessage} {stopwatch.ElapsedMilliseconds}");
                            else
                                _ = Log.TraceAsync($"Await Success: {topic} {stopwatch.ElapsedMilliseconds}");

                            if (!affirmation.Success)
                                exception = new AcknowledgementException(affirmation, topic);
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                        finally
                        {
                            waiter.Release();
                        }
                    };

                    await waiter.WaitAsync();
                    waiter.Dispose();

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
                            var factory = new ConnectionFactory() { HostName = host };
                            this.connection = factory.CreateConnection();
                            _ = Log.TraceAsync($"Sender Reconnected: {host}");
                        }
                    }
                }

                var channel = connection.CreateModel();

                string[][] claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var rabbitMessage = new RabbitMQEventMessage()
                {
                    Message = @event,
                    Claims = claims
                };

                var body = RabbitMQCommon.Serialize(rabbitMessage);
                if (encryptionKey != null)
                {
                    body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);
                }

                var exchange = @event.GetType().GetNiceName();

                channel.ExchangeDeclare(exchange, "fanout");

                var properties = channel.CreateBasicProperties();

                if (encryptionKey != null)
                {
                    var messageHeaders = new Dictionary<string, object>();
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
