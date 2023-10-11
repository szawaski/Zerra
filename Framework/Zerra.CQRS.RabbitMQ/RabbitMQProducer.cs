// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
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
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private IConnection connection = null;

        public string ConnectionString => host;

        public RabbitMQProducer(string host, SymmetricConfig symmetricConfig, string environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();
            try
            {
                var factory = new ConnectionFactory() { HostName = host };
                this.connection = factory.CreateConnection();
                _ = Log.InfoAsync($"{nameof(RabbitMQProducer)} Started");
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
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync();

            try
            {

                if (!String.IsNullOrWhiteSpace(environment))
                    topic = $"{environment}_{topic}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);

                try
                {
                    if (connection.IsOpen == false)
                    {
                        lock (locker)
                        {
                            if (connection.IsOpen == false)
                            {
                                var factory = new ConnectionFactory() { HostName = host };
                                this.connection = factory.CreateConnection();
                                _ = Log.InfoAsync($"Sender Reconnected");
                            }
                        }
                    }

                    using var channel = connection.CreateModel();

                    string[][] claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQCommandMessage()
                    {
                        Message = command,
                        Claims = claims,
                        Source = source
                    };

                    var body = RabbitMQCommon.Serialize(rabbitMessage);
                    if (symmetricConfig != null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    var properties = channel.CreateBasicProperties();

                    EventingBasicConsumer consumer = null;
                    string consumerTag = null;
                    string correlationId = null;
                    if (requireAcknowledgement)
                    {
                        var replyQueue = channel.QueueDeclare(String.Empty, false, true, true);
                        consumer = new EventingBasicConsumer(channel);
                        consumerTag = channel.BasicConsume(replyQueue.QueueName, true, consumer);

                        correlationId = Guid.NewGuid().ToString("N");
                        properties.ReplyTo = replyQueue.QueueName;
                        properties.CorrelationId = correlationId;
                    }

                    channel.BasicPublish(topic, String.Empty, properties, body);

                    if (requireAcknowledgement)
                    {
                        Exception exception = null;
                        using var waiter = new SemaphoreSlim(0, 1);

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

                        if (exception != null)
                        {
                            throw exception;
                        }
                    }

                    channel.Close();
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(null, ex);
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task SendAsync(IEvent @event, string source)
        {
            var eventType = @event.GetType();
            if (!topicsByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync();

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = $"{environment}_{topic}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);

                try
                {
                    if (connection.IsOpen == false)
                    {
                        lock (locker)
                        {
                            if (connection.IsOpen == false)
                            {
                                var factory = new ConnectionFactory() { HostName = host };
                                this.connection = factory.CreateConnection();
                                _ = Log.InfoAsync($"Sender Reconnected");
                            }
                        }
                    }

                    using var channel = connection.CreateModel();

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

                    var properties = channel.CreateBasicProperties();

                    channel.BasicPublish(topic, String.Empty, properties, body);

                    channel.Close();
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(null, ex);
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        public void Dispose()
        {
            this.connection.Close();
            this.connection.Dispose();
            GC.SuppressFinalize(this);
        }

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByCommandType.ContainsKey(type))
                return;
            topicsByCommandType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
        IEnumerable<Type> ICommandProducer.GetCommandTypes()
        {
            return topicsByCommandType.Keys;
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByEventType.ContainsKey(type))
                return;
            topicsByEventType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
        IEnumerable<Type> IEventProducer.GetEventTypes()
        {
            return topicsByEventType.Keys;
        }
    }
}
