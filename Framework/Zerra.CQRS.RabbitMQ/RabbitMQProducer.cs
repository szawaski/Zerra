﻿// Copyright © KaKush LLC
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
    public sealed class RabbitMQProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private readonly object locker = new();

        private readonly string host;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;
        private IConnection connection = null;

        public string ConnectionString => host;

        public RabbitMQProducer(string host, SymmetricConfig symmetricConfig, string environment)
        {
            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;
            try
            {
                var factory = new ConnectionFactory() { HostName = host };
                this.connection = factory.CreateConnection();
                _ = Log.TraceAsync($"{nameof(RabbitMQProducer)} Started");
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(null, ex);
                throw;
            }
        }

        Task ICommandProducer.DispatchAsync(ICommand command, string source) { return SendAsync(command, false, source); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command, string source) { return SendAsync(command, true, source); }
        Task IEventProducer.DispatchAsync(IEvent @event, string source) { return SendAsync(@event, source); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement, string source)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                if (connection.IsOpen == false)
                {
                    lock (locker)
                    {
                        if (connection.IsOpen == false)
                        {
                            var factory = new ConnectionFactory() { HostName = host };
                            this.connection = factory.CreateConnection();
                            _ = Log.TraceAsync($"Sender Reconnected");
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
                if (symmetricConfig != null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                string topic;
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = $"{environment}_{command.GetType().GetNiceName()}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    topic = command.GetType().GetNiceName().Truncate(RabbitMQCommon.TopicMaxLength);

                var properties = channel.CreateBasicProperties();

                EventingBasicConsumer consumer = null;
                string consumerTag = null;
                string correlationId = null;
                if (requireAcknowledgement)
                {
                    var replyQueueName = channel.QueueDeclare().QueueName;
                    consumer = new EventingBasicConsumer(channel);
                    consumerTag = channel.BasicConsume(replyQueueName, false, consumer);

                    correlationId = Guid.NewGuid().ToString("N");
                    properties.ReplyTo = replyQueueName;
                    properties.CorrelationId = correlationId;
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

                            var acknowledgementBody = e.Body.Span;
                            if (symmetricConfig != null)
                                acknowledgementBody = SymmetricEncryptor.Decrypt(symmetricConfig, acknowledgementBody);
                            
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
                            _ = waiter.Release();
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

        private Task SendAsync(IEvent @event, string source)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                if (connection.IsOpen == false)
                {
                    lock (locker)
                    {
                        if (connection.IsOpen == false)
                        {
                            var factory = new ConnectionFactory() { HostName = host };
                            this.connection = factory.CreateConnection();
                            _ = Log.TraceAsync($"Sender Reconnected");
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
                    Claims = claims,
                    Source = source
                };

                var body = RabbitMQCommon.Serialize(rabbitMessage);
                if (symmetricConfig != null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                string topic;
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = $"{environment}_{@event.GetType().GetNiceName()}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    topic = @event.GetType().GetNiceName().Truncate(RabbitMQCommon.TopicMaxLength);

                channel.ExchangeDeclare(topic, "fanout");

                var properties = channel.CreateBasicProperties();

                channel.BasicPublish(topic, String.Empty, properties, body);

                _ = Log.TraceAsync($"Sent: {topic}");

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
            GC.SuppressFinalize(this);
        }
    }
}
