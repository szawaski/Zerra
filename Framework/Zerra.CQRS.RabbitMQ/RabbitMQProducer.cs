// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Data;
using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.CQRS.RabbitMQ
{
    /// <summary>
    /// RabbitMQ implementation of command and event producer for distributed CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Provides high-performance, reliable message delivery to RabbitMQ exchanges.
    /// Supports command acknowledgements with automatic connection recovery and optional message encryption.
    /// Thread-safe for concurrent operations with configurable throttling per topic.
    /// </remarks>
    public sealed class RabbitMQProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private readonly Lock locker = new();

        private readonly string host;
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ILogger? log;
        private readonly string? environment;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private readonly ConnectionFactory factory;
        private IConnection? connection = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQProducer"/> class.
        /// </summary>
        /// <param name="host">The RabbitMQ server hostname or IP address.</param>
        /// <param name="serializer">The serializer for message serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to prefix exchange names for isolation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public RabbitMQProducer(string host, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.log = log;
            this.environment = environment;
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();

            this.factory = new ConnectionFactory() { HostName = host };
            try
            {
                this.connection = factory.CreateConnection();
            }
            catch (Exception ex)
            {
                log?.Error(ex);
                throw;
            }
        }

        string ICommandProducer.MessageHost => "[Host has Secrets]";
        string IEventProducer.MessageHost => "[Host has Secrets]";

        Task ICommandProducer.DispatchAsync(ICommand command, string source, CancellationToken cancellationToken) => SendAsync(command, false, source, cancellationToken);
        Task ICommandProducer.DispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken) => SendAsync(command, true, source, cancellationToken);
        Task<TResult> ICommandProducer.DispatchAwaitAsync<TResult>(ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default => SendAsync(command, source, cancellationToken);
        Task IEventProducer.DispatchAsync(IEvent @event, string source, CancellationToken cancellationToken) => SendAsync(@event, source, cancellationToken);

        private async Task SendAsync(ICommand command, bool requireAcknowledgement, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.Name} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);

                try
                {
                    if (connection is null || connection!.IsOpen == false)
                    {
                        lock (locker)
                        {
                            if (connection is null || connection.IsOpen == false)
                            {
                                this.connection?.Close();
                                this.connection?.Dispose();
                                this.connection = factory.CreateConnection();
                                log?.Info($"Sender Reconnected");
                            }
                        }
                    }

                    using var channel = connection.CreateModel();

                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = serializer.SerializeBytes(command),
                        MessageType = command.GetType(),
                        HasResult = false,
                        Claims = claims,
                        Source = source
                    };

                    var body = serializer.SerializeBytes(rabbitMessage);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    var properties = channel.CreateBasicProperties();

                    EventingBasicConsumer? consumer = null;
                    string? consumerTag = null;
                    string? correlationId = null;
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
                        Acknowledgement? acknowledgement = null;
                        using var waiter = new SemaphoreSlim(0, 1);

                        consumer!.Received += (sender, e) =>
                        {
                            try
                            {
                                if (e.BasicProperties.CorrelationId != correlationId)
                                    throw new Exception("ACK response CorrelationIds should be single and unique");

                                channel.BasicCancel(consumerTag);

                                var acknowledgementBody = e.Body.Span;
                                if (encryptor is not null)
                                    acknowledgementBody = encryptor.Decrypt(acknowledgementBody);

                                acknowledgement = serializer.Deserialize<Acknowledgement>(acknowledgementBody);
                                acknowledgement ??= new Acknowledgement("Invalid Acknowledgement");
                            }
                            catch (Exception ex)
                            {
                                acknowledgement = new Acknowledgement(ex.Message);
                            }
                            finally
                            {
                                _ = waiter.Release();
                            }
                        };

                        await waiter.WaitAsync(cancellationToken);

                        Acknowledgement.ThrowIfFailed(acknowledgement);
                    }

                    channel.Close();
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                    throw;
                }
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        private async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.Name} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);

                try
                {
                    if (connection is null || connection!.IsOpen == false)
                    {
                        lock (locker)
                        {
                            if (connection is null || connection.IsOpen == false)
                            {
                                this.connection?.Close();
                                this.connection?.Dispose();
                                this.connection = factory.CreateConnection();
                                log?.Info($"Sender Reconnected");
                            }
                        }
                    }

                    using var channel = connection.CreateModel();

                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = serializer.SerializeBytes(command),
                        MessageType = command.GetType(),
                        HasResult = true,
                        Claims = claims,
                        Source = source
                    };

                    var body = serializer.SerializeBytes(rabbitMessage);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    var properties = channel.CreateBasicProperties();

                    var replyQueue = channel.QueueDeclare(String.Empty, false, true, true);
                    var consumer = new EventingBasicConsumer(channel);
                    var consumerTag = channel.BasicConsume(replyQueue.QueueName, true, consumer);

                    var correlationId = Guid.NewGuid().ToString("N");
                    properties.ReplyTo = replyQueue.QueueName;
                    properties.CorrelationId = correlationId;

                    channel.BasicPublish(topic, String.Empty, properties, body);

                    Acknowledgement? acknowledgement = null;
                    using var waiter = new SemaphoreSlim(0, 1);

                    consumer!.Received += (sender, e) =>
                    {
                        try
                        {
                            if (e.BasicProperties.CorrelationId != correlationId)
                                throw new Exception("ACK response CorrelationIds should be single and unique");

                            channel.BasicCancel(consumerTag);

                            var acknowledgementBody = e.Body.Span;
                            if (encryptor is not null)
                                acknowledgementBody = encryptor.Decrypt(acknowledgementBody);

                            acknowledgement = serializer.Deserialize<Acknowledgement>(acknowledgementBody);
                            acknowledgement ??= new Acknowledgement("Invalid Acknowledgement");
                        }
                        catch (Exception ex)
                        {
                            acknowledgement = new Acknowledgement(ex.Message);
                        }
                        finally
                        {
                            _ = waiter.Release();
                        }
                    };

                    await waiter.WaitAsync(cancellationToken);

                    var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(acknowledgement)!;

                    channel.Close();

                    return result;
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                    throw;
                }
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        private async Task SendAsync(IEvent @event, string source, CancellationToken cancellationToken)
        {
            var eventType = @event.GetType();
            if (!topicsByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.Name} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.Name} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);

                try
                {
                    if (connection is null || connection.IsOpen == false)
                    {
                        lock (locker)
                        {
                            if (connection is null || connection.IsOpen == false)
                            {
                                this.connection?.Close();
                                this.connection?.Dispose();
                                this.connection = factory.CreateConnection();
                                log?.Info($"Sender Reconnected");
                            }
                        }
                    }

                    using var channel = connection.CreateModel();

                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = serializer.SerializeBytes(@event),
                        MessageType = @event.GetType(),
                        HasResult = false,
                        Claims = claims,
                        Source = source
                    };

                    var body = serializer.SerializeBytes(rabbitMessage);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    var properties = channel.CreateBasicProperties();

                    channel.BasicPublish(topic, String.Empty, properties, body);

                    channel.Close();
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                    throw;
                }
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RabbitMQProducer"/>.
        /// </summary>
        /// <remarks>
        /// Closes and disposes the RabbitMQ connection. After disposal, the producer cannot be used.
        /// </remarks>
        public void Dispose()
        {
            this.connection?.Close();
            this.connection?.Dispose();
            GC.SuppressFinalize(this);
        }

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByCommandType.ContainsKey(type))
                return;
            _ = topicsByCommandType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByEventType.ContainsKey(type))
                return;
            _ = topicsByEventType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
    }
}
