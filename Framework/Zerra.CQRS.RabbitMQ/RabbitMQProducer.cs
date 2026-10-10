// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using System.Data;
using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Compression;
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
        //RabbitMQ's direct reply-to: a reply comes straight back to the consumer on the channel that published, with no reply queue to declare
        private const string directReplyTo = "amq.rabbitmq.reply-to";

        //guards the connection and the channel, a channel isn't safe to publish on from more than one thread
        private readonly SemaphoreSlim locker = new(1, 1);

        private readonly string host;
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ILogger? log;
        private readonly string? environment;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Acknowledgement>> ackCallbacks;
        //the topics this producer has declared, only used under the lock
        private readonly HashSet<string> declaredTopics;
        private readonly ConnectionFactory factory;
        private IConnection? connection = null;
        private IChannel? channel = null;
        private volatile bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQProducer"/> class.
        /// </summary>
        /// <param name="host">The RabbitMQ server hostname or IP address, or an AMQP URI (amqp://user:password@host:port/vhost, amqps:// for TLS) to also configure credentials, port, virtual host, and TLS.</param>
        /// <param name="serializer">The serializer for message serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to prefix exchange names for isolation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public RabbitMQProducer(string host, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.log = log;
            this.environment = environment;
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();
            this.ackCallbacks = new();
            this.declaredTopics = new();

            this.factory = RabbitMQCommon.CreateConnectionFactory(host);
            //opened now so the first send doesn't wait for the channel and its reply consumer, a send retries if this fails
            _ = Task.Run(async () =>
            {
                await locker.WaitAsync();
                try
                {
                    if (this.channel is null || !this.channel.IsOpen)
                        _ = await OpenChannelAsync();
                }
                catch (Exception ex)
                {
                    //disposed before it finished
                    if (!disposed)
                        log?.Error(ex);
                }
                finally
                {
                    _ = locker.Release();
                }
            });
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
                try
                {
                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = serializer.SerializeBytes(command, command.GetType()),
                        MessageType = commandType.Name,
                        HasResult = false,
                        Claims = claims,
                        Source = source
                    };

                    var body = serializer.SerializeBytes(rabbitMessage);
                    if (compressor is not null)
                        body = compressor.Compress(body);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    if (requireAcknowledgement)
                    {
                        var correlationId = Guid.NewGuid().ToString("N");

                        //completed by the reply consumer, continuations run off its thread so it goes back to receiving
                        var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                        try
                        {
                            //added before publishing, a reply that arrives first would otherwise be missed
                            _ = ackCallbacks.TryAdd(correlationId, waiter);

                            var properties = new BasicProperties() { ReplyTo = directReplyTo, CorrelationId = correlationId };
                            await locker.WaitAsync(cancellationToken);
                            try
                            {
                                var channel = this.channel is not null && this.channel.IsOpen ? this.channel : await OpenChannelAsync();
                                await DeclareTopicAsync(channel, topic, true);
                                await channel.BasicPublishAsync(topic, String.Empty, false, properties, body);
                            }
                            finally
                            {
                                _ = locker.Release();
                            }

                            Acknowledgement acknowledgement;
                            using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                                acknowledgement = await waiter.Task;

                            Acknowledgement.ThrowIfFailed(commandType.Name, serializer, acknowledgement);
                        }
                        finally
                        {
                            _ = ackCallbacks.TryRemove(correlationId, out _);
                        }
                    }
                    else
                    {
                        await locker.WaitAsync(cancellationToken);
                        try
                        {
                            var channel = this.channel is not null && this.channel.IsOpen ? this.channel : await OpenChannelAsync();
                            await DeclareTopicAsync(channel, topic, true);
                            await channel.BasicPublishAsync(topic, String.Empty, body);
                        }
                        finally
                        {
                            _ = locker.Release();
                        }
                    }
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
                try
                {
                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = serializer.SerializeBytes(command, command.GetType()),
                        MessageType = commandType.Name,
                        HasResult = true,
                        Claims = claims,
                        Source = source
                    };

                    var body = serializer.SerializeBytes(rabbitMessage);
                    if (compressor is not null)
                        body = compressor.Compress(body);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    var correlationId = Guid.NewGuid().ToString("N");

                    //completed by the reply consumer, continuations run off its thread so it goes back to receiving
                    var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                    try
                    {
                        //added before publishing, a reply that arrives first would otherwise be missed
                        _ = ackCallbacks.TryAdd(correlationId, waiter);

                        var properties = new BasicProperties() { ReplyTo = directReplyTo, CorrelationId = correlationId };
                        await locker.WaitAsync(cancellationToken);
                        try
                        {
                            var channel = this.channel is not null && this.channel.IsOpen ? this.channel : await OpenChannelAsync();
                            await DeclareTopicAsync(channel, topic, true);
                            await channel.BasicPublishAsync(topic, String.Empty, false, properties, body);
                        }
                        finally
                        {
                            _ = locker.Release();
                        }

                        Acknowledgement acknowledgement;
                        using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                            acknowledgement = await waiter.Task;

                        var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(commandType.Name, serializer, acknowledgement, typeof(TResult))!;

                        return result;
                    }
                    finally
                    {
                        _ = ackCallbacks.TryRemove(correlationId, out _);
                    }
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
                try
                {
                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = serializer.SerializeBytes(@event, @event.GetType()),
                        MessageType = eventType.Name,
                        HasResult = false,
                        Claims = claims,
                        Source = source
                    };

                    var body = serializer.SerializeBytes(rabbitMessage);
                    if (compressor is not null)
                        body = compressor.Compress(body);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    await locker.WaitAsync(cancellationToken);
                    try
                    {
                        var channel = this.channel is not null && this.channel.IsOpen ? this.channel : await OpenChannelAsync();
                        await DeclareTopicAsync(channel, topic, false);
                        await channel.BasicPublishAsync(topic, String.Empty, body);
                    }
                    finally
                    {
                        _ = locker.Release();
                    }
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

        //Called under the lock when there's no open channel. One channel is used for the producer's life instead of one per message, which saves
        //opening and closing it with the broker on every send, and the replies to it arrive through direct reply-to on the consumer started here.
        private async Task<IChannel> OpenChannelAsync()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(RabbitMQProducer));

            var connection = this.connection;
            if (connection is null || !connection.IsOpen)
            {
                var reconnect = connection is not null;
                if (connection is not null)
                    await connection.DisposeAsync();
                connection = await factory.CreateConnectionAsync();
                this.connection = connection;
                if (reconnect)
                    log?.Info($"Sender Reconnected");
            }

            if (this.channel is not null)
                await this.channel.DisposeAsync();

            var channel = await connection.CreateChannelAsync();

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (sender, e) =>
            {
                var correlationId = e.BasicProperties.CorrelationId;
                if (correlationId is null || !ackCallbacks.TryRemove(correlationId, out var waiter))
                    return Task.CompletedTask;

                Acknowledgement? acknowledgement;
                try
                {
                    //the body is only valid during this callback, so it's read here
                    var acknowledgementBody = e.Body.Span;
                    if (encryptor is not null)
#if NETSTANDARD2_0
                        acknowledgementBody = encryptor.Decrypt(acknowledgementBody.ToArray());
#else
                        acknowledgementBody = encryptor.Decrypt(acknowledgementBody);
#endif
                    if (compressor is not null)
#if NETSTANDARD2_0
                        acknowledgementBody = compressor.Decompress(acknowledgementBody.ToArray());
#else
                        acknowledgementBody = compressor.Decompress(acknowledgementBody);
#endif

                    acknowledgement = serializer.Deserialize<Acknowledgement>(acknowledgementBody);
                    acknowledgement ??= new Acknowledgement(serializer, "Invalid Acknowledgement");
                }
                catch (Exception ex)
                {
                    acknowledgement = new Acknowledgement(serializer, ex.Message);
                }

                _ = waiter.TrySetResult(acknowledgement);
                return Task.CompletedTask;
            };

            //a direct reply only reaches the channel that published the command, once it closes the replies still awaited can't arrive
            channel.ChannelShutdownAsync += (sender, e) =>
            {
                foreach (var correlationId in ackCallbacks.Keys)
                {
                    if (ackCallbacks.TryRemove(correlationId, out var waiter))
                        _ = waiter.TrySetException(new Exception($"{nameof(RabbitMQProducer)} channel closed before the acknowledgement arrived: {e.ReplyText}"));
                }
                return Task.CompletedTask;
            };

            //direct reply-to requires no acknowledgements, and the consumer before publishing
            _ = await channel.BasicConsumeAsync(directReplyTo, true, consumer);

            _ = Interlocked.Exchange(ref this.channel, channel);
            //disposed while opening, Dispose may have missed this channel and connection
            if (disposed)
            {
                Interlocked.Exchange(ref this.channel, null)?.Dispose();
                Interlocked.Exchange(ref this.connection, null)?.Dispose();
                throw new ObjectDisposedException(nameof(RabbitMQProducer));
            }
            return channel;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RabbitMQProducer"/>.
        /// </summary>
        /// <remarks>
        /// Closes and disposes the RabbitMQ channel and connection. After disposal, the producer cannot be used.
        /// </remarks>
        public void Dispose()
        {
            disposed = true;
            //disposing closes them, ignoring ones the broker or network already closed
            Interlocked.Exchange(ref this.channel, null)?.Dispose();
            Interlocked.Exchange(ref this.connection, null)?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="RabbitMQProducer"/>.
        /// </summary>
        /// <remarks>
        /// Closes and disposes the RabbitMQ channel and connection. After disposal, the producer cannot be used.
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            disposed = true;
            //disposing closes them, ignoring ones the broker or network already closed
            var channel = Interlocked.Exchange(ref this.channel, null);
            if (channel is not null)
                await channel.DisposeAsync();
            var connection = Interlocked.Exchange(ref this.connection, null);
            if (connection is not null)
                await connection.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByCommandType.ContainsKey(type))
                return;
            topic = BuildTopic(topic, "command");
            _ = topicsByCommandType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            //declared now so the first send finds it declared
            _ = Task.Run(async () =>
            {
                await locker.WaitAsync();
                try
                {
                    await DeclareTopicAsync(this.channel is not null && this.channel.IsOpen ? this.channel : await OpenChannelAsync(), topic, true);
                }
                catch (Exception ex)
                {
                    //disposed before it finished
                    if (!disposed)
                        log?.Error(ex);
                }
                finally
                {
                    _ = locker.Release();
                }
            });
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByEventType.ContainsKey(type))
                return;
            topic = BuildTopic(topic, "event");
            _ = topicsByEventType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            //declared now so the first send finds it declared
            _ = Task.Run(async () =>
            {
                await locker.WaitAsync();
                try
                {
                    await DeclareTopicAsync(this.channel is not null && this.channel.IsOpen ? this.channel : await OpenChannelAsync(), topic, false);
                }
                catch (Exception ex)
                {
                    //disposed before it finished
                    if (!disposed)
                        log?.Error(ex);
                }
                finally
                {
                    _ = locker.Release();
                }
            });
        }

        //Called under the lock. The exchange, and for commands the queue, are declared the same as the consumers declare them, so a command sent
        //before any consumer has run waits in the queue instead of being dropped when the broker closes the channel for a missing exchange.
        private async ValueTask DeclareTopicAsync(IChannel channel, string topic, bool isCommand)
        {
            if (declaredTopics.Contains(topic))
                return;
            if (isCommand)
            {
                await channel.ExchangeDeclareAsync(topic, ExchangeType.Direct);
                _ = await channel.QueueDeclareAsync(topic, true, false, false);
                await channel.QueueBindAsync(topic, topic, String.Empty);
            }
            else
            {
                await channel.ExchangeDeclareAsync(topic, ExchangeType.Fanout);
            }
            _ = declaredTopics.Add(topic);
        }

        private string BuildTopic(string topic, string kind)
        {
            bool truncated;
            if (!String.IsNullOrWhiteSpace(environment))
                topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic, out truncated);
            else
                topic = topic.Truncate(RabbitMQCommon.TopicMaxLength, out truncated);
            if (truncated)
                log?.Warn($"{nameof(RabbitMQProducer)} truncated the {kind} exchange to {RabbitMQCommon.TopicMaxLength} characters: {topic}. Another exchange truncating to the same name would receive these messages.");
            return topic;
        }
    }
}
