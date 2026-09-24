// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.Kafka
{
    /// <summary>
    /// Kafka implementation of command and event producer for distributed CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Provides high-performance, reliable message delivery to Kafka topics.
    /// Supports command acknowledgements with automatic retry logic and optional message encryption.
    /// Supports SASL authentication when username and password are provided.
    /// Thread-safe for concurrent operations.
    /// </remarks>
    public sealed class KafkaProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private bool listenerStarted = false;
        private readonly SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly string host;
        private readonly Zerra.Serialization.ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ILogger? log;
        private readonly string? environment;
        private readonly string? userName;
        private readonly string? password;

        private readonly string ackTopic;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private readonly IProducer<string, byte[]> producer;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
        /// </summary>
        /// <param name="host">The Kafka bootstrap server address (e.g., "localhost:9092").</param>
        /// <param name="serializer">The serializer for message serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to prefix topic names for isolation.</param>
        /// <param name="userName">Optional username for SASL authentication. Must be paired with password.</param>
        /// <param name="password">Optional password for SASL authentication. Must be paired with userName.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        //Confluent.Kafka binds its native library by finding these methods and fields through reflection, which native AOT would otherwise trim away
#if !NETSTANDARD2_0
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields, "Confluent.Kafka.Impl.Librdkafka", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods_Alpine", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods_Centos8", "Confluent.Kafka")]
#endif
        public KafkaProducer(string host, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, string? userName, string? password)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.log = log;
            this.environment = environment;
            this.userName = userName;
            this.password = password;

            var entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            var clientID = StringExtensions.Join(KafkaCommon.TopicMaxLength - 4 - 33, "_", environment ?? "Unknown_Environment", Environment.MachineName, entryAssemblyName ?? "Unknown_Assembly", out var clientIDTruncated);
            if (clientIDTruncated)
                log?.Warn($"{nameof(KafkaProducer)} truncated the client ID to {KafkaCommon.TopicMaxLength - 4 - 33} characters: {clientID}. It only names the producer in the broker's logs, the acknowledgement topic it prefixes is still unique.");
            //unique per producer, instances of the same app on the same machine would otherwise share a group and only one would receive the acknowledgements
            this.ackTopic = $"ACK-{clientID}_{Guid.NewGuid():N}";
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();

            var producerConfig = new ProducerConfig();
            producerConfig.BootstrapServers = host;
            producerConfig.ClientId = clientID;
            if (userName is not null && password is not null)
            {
                producerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                producerConfig.SaslMechanism = SaslMechanism.Plain;
                producerConfig.SaslUsername = userName;
                producerConfig.SaslPassword = password;
            }

            producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
        }

        //the acknowledgement topic and consumer group name, for tests to clean up
        internal string AckTopic => ackTopic;

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
                throw new Exception($"{commandType.Name} is not registered with {nameof(KafkaProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(KafkaProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (requireAcknowledgement)
                {
                    if (!listenerStarted)
                    {
                        try
                        {
                            await listenerStartedLock.WaitAsync(cancellationToken);

                            if (!listenerStarted)
                            {
                                await KafkaCommon.EnsureTopic(host, userName, password, ackTopic);
                                _ = Task.Run(AckListeningThread);
                                listenerStarted = true;
                            }
                        }
                        finally
                        {
                            _ = listenerStartedLock.Release();
                        }
                    }
                }

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new KafkaMessage()
                {
                    MessageData = serializer.SerializeBytes(command, command.GetType()),
                    MessageType = commandType.AssemblyQualifiedName,
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                if (requireAcknowledgement)
                {
                    var ackKey = Guid.NewGuid().ToString("N");

                    var headers = new Headers();
                    headers.Add(new Header(KafkaCommon.AckTopicHeader, Encoding.UTF8.GetBytes(ackTopic)));
                    headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                    var key = KafkaCommon.MessageWithAckKey;

                    var waiter = new SemaphoreSlim(0, 1);

                    try
                    {
                        Acknowledgement? acknowledgement = null;
                        _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                        {
                            acknowledgement = ackFromCallback;
                            _ = waiter.Release();
                        });

                        var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body }, cancellationToken);
                        if (producerResult.Status != PersistenceStatus.Persisted)
                            throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                        await waiter.WaitAsync(cancellationToken);

                        Acknowledgement.ThrowIfFailed(commandType.Name, serializer, acknowledgement);
                    }
                    finally
                    {
                        _ = ackCallbacks.TryRemove(ackKey, out _);
                        waiter?.Dispose();
                    }
                }
                else
                {
                    var key = KafkaCommon.MessageKey;

                    var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = key, Value = body });
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");
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
                throw new Exception($"{commandType.Name} is not registered with {nameof(KafkaProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(KafkaProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!listenerStarted)
                {
                    try
                    {
                        await listenerStartedLock.WaitAsync(cancellationToken);

                        if (!listenerStarted)
                        {
                            await KafkaCommon.EnsureTopic(host, userName, password, ackTopic);
                            _ = Task.Run(AckListeningThread);
                            listenerStarted = true;
                        }
                    }
                    finally
                    {
                        _ = listenerStartedLock.Release();
                    }
                }

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new KafkaMessage()
                {
                    MessageData = serializer.SerializeBytes(command, command.GetType()),
                    MessageType = commandType.AssemblyQualifiedName,
                    HasResult = true,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var ackKey = Guid.NewGuid().ToString("N");

                var headers = new Headers();
                headers.Add(new Header(KafkaCommon.AckTopicHeader, Encoding.UTF8.GetBytes(ackTopic)));
                headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                var key = KafkaCommon.MessageWithAckKey;

                var waiter = new SemaphoreSlim(0, 1);

                try
                {
                    Acknowledgement? acknowledgement = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        acknowledgement = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body }, cancellationToken);
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                    await waiter.WaitAsync(cancellationToken);

                    var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(commandType.Name, serializer, acknowledgement)!;

                    return result;
                }
                finally
                {
                    _ = ackCallbacks.TryRemove(ackKey, out _);
                    waiter?.Dispose();
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
                throw new Exception($"{eventType.Name} is not registered with {nameof(KafkaProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.Name} is not registered with {nameof(KafkaProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new KafkaMessage()
                {
                    MessageData = serializer.SerializeBytes(@event, @event.GetType()),
                    MessageType = eventType.AssemblyQualifiedName,
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = KafkaCommon.MessageKey, Value = body }, cancellationToken);
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        private async Task AckListeningThread()
        {
            var consumerConfig = new ConsumerConfig();
            consumerConfig.BootstrapServers = host;
            consumerConfig.GroupId = ackTopic;
            consumerConfig.EnableAutoCommit = false;
            //the topic is new and only this producer's, so reading from the start catches acknowledgements sent before the subscription was assigned
            consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
            if (userName is not null && password is not null)
            {
                consumerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                consumerConfig.SaslMechanism = SaslMechanism.Plain;
                consumerConfig.SaslUsername = userName;
                consumerConfig.SaslPassword = password;
            }

            //the retry stays inside the outer try, a goto out of it would run the finally and dispose the canceller on a transient error
            try
            {
            retry:

                try
                {
                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                    {
                        consumer.Subscribe(ackTopic);
                        try
                        {
                            for (; ; )
                            {
                                var consumerResult = consumer.Consume(canceller.Token);
                                consumer.Commit(consumerResult);

                                if (!ackCallbacks.TryRemove(consumerResult.Message.Key, out var callback))
                                    continue;

                                Acknowledgement? acknowledgement = null;
                                try
                                {
                                    var response = consumerResult.Message.Value;
                                    if (encryptor is not null)
                                        response = encryptor.Decrypt(response);
                                    acknowledgement = serializer.Deserialize<Acknowledgement>(response);
                                    acknowledgement ??= new Acknowledgement(serializer, "Invalid Acknowledgement");
                                }
                                catch (Exception ex)
                                {
                                    acknowledgement = new Acknowledgement(serializer, ex.Message);
                                }

                                callback(acknowledgement);

                                if (canceller.IsCancellationRequested)
                                    break;
                            }
                        }
                        finally
                        {
                            //Close leaves the group right away, Unsubscribe and Dispose leave its member until the session times out
                            consumer.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(ex);
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
            }
            finally
            {
                listenerStarted = false;

                try
                {
                    await KafkaCommon.DeleteTopic(host, userName, password, ackTopic);
                    //the acknowledgement consumer's group is named after the topic and goes with it
                    await KafkaCommon.DeleteConsumerGroup(host, userName, password, ackTopic);
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
                canceller.Dispose();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="KafkaProducer"/>.
        /// </summary>
        /// <remarks>
        /// Cancels the acknowledgement listener, disposes the Kafka producer, and releases semaphore resources.
        /// </remarks>
        public void Dispose()
        {
            canceller.Cancel();
            producer.Dispose();
            listenerStartedLock.Dispose();
            canceller.Dispose();
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
        }

        private string BuildTopic(string topic, string kind)
        {
            bool truncated;
            if (!String.IsNullOrWhiteSpace(environment))
                topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic, out truncated);
            else
                topic = topic.Truncate(KafkaCommon.TopicMaxLength, out truncated);
            if (truncated)
                log?.Warn($"{nameof(KafkaProducer)} truncated the {kind} topic to {KafkaCommon.TopicMaxLength} characters: {topic}. Another topic truncating to the same name would receive these messages.");
            return topic;
        }
    }
}
