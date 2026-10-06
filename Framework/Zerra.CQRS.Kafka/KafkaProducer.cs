// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using Zerra.Compression;
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
        private Task? ackListening = null;
        private readonly SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly Zerra.Serialization.ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ILogger? log;
        private readonly string? environment;

        private readonly string ackTopic;
        private readonly KafkaCommonHost commonHost;
        private readonly byte[] ackTopicBytes;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private readonly ConcurrentDictionary<string, Lazy<Task>> recoveryByTopic;
        private readonly IProducer<string, byte[]> producer;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Acknowledgement>> ackCallbacks;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducer"/> class.
        /// </summary>
        /// <param name="host">The Kafka bootstrap server address (e.g., "localhost:9092").</param>
        /// <param name="serializer">The serializer for message serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="log">Optional logger for diagnostic information and errors.</param>
        /// <param name="environment">Optional environment name to prefix topic names for isolation.</param>
        /// <param name="userName">Optional username for SASL authentication. Must be paired with password.</param>
        /// <param name="password">Optional password for SASL authentication. Must be paired with userName.</param>
        /// <param name="useTls">True to connect with TLS, SASL_SSL with a user name and password or SSL without.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        //Confluent.Kafka binds its native library by finding these methods and fields through reflection, which native AOT would otherwise trim away
#if !NETSTANDARD2_0
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields, "Confluent.Kafka.Impl.Librdkafka", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods_Alpine", "Confluent.Kafka")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Confluent.Kafka.Impl.NativeMethods.NativeMethods_Centos8", "Confluent.Kafka")]
#endif
        public KafkaProducer(string host, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment, string? userName, string? password, bool useTls = false)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.log = log;
            this.environment = environment;
            this.commonHost = KafkaCommon.GetHost(host, userName, password, useTls);

            var entryAssemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
            var clientID = StringExtensions.Join(KafkaCommon.TopicMaxLength - 4 - 33, "_", environment ?? "Unknown_Environment", Environment.MachineName, entryAssemblyName ?? "Unknown_Assembly", out var clientIDTruncated);
            if (clientIDTruncated)
                log?.Warn($"{nameof(KafkaProducer)} truncated the client ID to {KafkaCommon.TopicMaxLength - 4 - 33} characters: {clientID}. It only names the producer in the broker's logs, the acknowledgement topic it prefixes is still unique.");
            //unique per producer, instances of the same app on the same machine would otherwise share a group and only one would receive the acknowledgements
            this.ackTopic = $"ACK-{clientID}_{Guid.NewGuid():N}";
            this.ackTopicBytes = Encoding.UTF8.GetBytes(this.ackTopic);
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();
            this.recoveryByTopic = new();

            var producerConfig = new ProducerConfig();
            producerConfig.BootstrapServers = host;
            producerConfig.LingerMs = 0;
            //topics are created by Zerra with its settings, not by the broker with its defaults
            producerConfig.AllowAutoCreateTopics = false;
            //a send to a missing topic fails after this instead of the 30 second default, then creates it
            producerConfig.TopicMetadataPropagationMaxMs = 1000;
            producerConfig.ClientId = clientID;
            if (userName is not null && password is not null)
            {
                producerConfig.SecurityProtocol = useTls ? SecurityProtocol.SaslSsl : SecurityProtocol.SaslPlaintext;
                producerConfig.SaslMechanism = SaslMechanism.Plain;
                producerConfig.SaslUsername = userName;
                producerConfig.SaslPassword = password;
            }
            else if (useTls)
            {
                producerConfig.SecurityProtocol = SecurityProtocol.Ssl;
            }

            producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, TaskCompletionSource<Acknowledgement>>();
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
                        await listenerStartedLock.WaitAsync(cancellationToken);
                        try
                        {
                            if (!listenerStarted)
                            {
                                await KafkaCommon.CreateTopic(commonHost, ackTopic);
                                ackListening = Task.Run(AckListeningThread);
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
                    MessageType = commandType.Name,
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (compressor is not null)
                    body = compressor.Compress(body);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                if (requireAcknowledgement)
                {
                    var ackKey = Guid.NewGuid().ToString("N");

                    var headers = new Headers();
                    headers.Add(new Header(KafkaCommon.AckTopicHeader, ackTopicBytes));
                    headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                    var key = KafkaCommon.MessageWithAckKey;

                    var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                    try
                    {
                        _ = ackCallbacks.TryAdd(ackKey, waiter);

                        var producerResult = await ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body }, cancellationToken);
                        if (producerResult.Status != PersistenceStatus.Persisted)
                            throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                        Acknowledgement acknowledgement;
                        using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                            acknowledgement = await waiter.Task;

                        Acknowledgement.ThrowIfFailed(commandType.Name, serializer, acknowledgement);
                    }
                    finally
                    {
                        _ = ackCallbacks.TryRemove(ackKey, out _);
                    }
                }
                else
                {
                    var key = KafkaCommon.MessageKey;

                    var producerResult = await ProduceAsync(topic, new Message<string, byte[]> { Key = key, Value = body }, cancellationToken);
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
                    await listenerStartedLock.WaitAsync(cancellationToken);
                    try
                    {
                        if (!listenerStarted)
                        {
                            await KafkaCommon.CreateTopic(commonHost, ackTopic);
                            ackListening = Task.Run(AckListeningThread);
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
                    MessageType = commandType.Name,
                    HasResult = true,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (compressor is not null)
                    body = compressor.Compress(body);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var ackKey = Guid.NewGuid().ToString("N");

                var headers = new Headers();
                headers.Add(new Header(KafkaCommon.AckTopicHeader, ackTopicBytes));
                headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                var key = KafkaCommon.MessageWithAckKey;

                var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                try
                {
                    _ = ackCallbacks.TryAdd(ackKey, waiter);

                    var producerResult = await ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body }, cancellationToken);
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                    Acknowledgement acknowledgement;
                    using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                        acknowledgement = await waiter.Task;

                    var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(commandType.Name, serializer, acknowledgement, typeof(TResult))!;

                    return result;
                }
                finally
                {
                    _ = ackCallbacks.TryRemove(ackKey, out _);
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
                    MessageType = eventType.Name,
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (compressor is not null)
                    body = compressor.Compress(body);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var producerResult = await ProduceAsync(topic, new Message<string, byte[]> { Key = KafkaCommon.MessageKey, Value = body }, cancellationToken);
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        //topics aren't ensured up front for faster startup, only after a send fails
        private async Task<DeliveryResult<string, byte[]>> ProduceAsync(string topic, Message<string, byte[]> message, CancellationToken cancellationToken)
        {
            try
            {
                return await producer.ProduceAsync(topic, message, cancellationToken);
            }
            catch (ProduceException<string, byte[]> ex) when (ex.Error.Code == ErrorCode.Local_UnknownTopic || ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                log?.Warn($"{nameof(KafkaProducer)} failed to send to {topic}, ensuring it and trying again: {ex.Error.Code} {ex.Message}");
            }

            //sends failing at the same time share one recovery
            var recovery = recoveryByTopic.GetOrAdd(topic, _ => new Lazy<Task>(() => RecoverAsync(topic)));
            try
            {
                await recovery.Value;
            }
            finally
            {
                _ = ((ICollection<KeyValuePair<string, Lazy<Task>>>)recoveryByTopic).Remove(new(topic, recovery));
            }

            return await producer.ProduceAsync(topic, message, cancellationToken);
        }

        private async Task RecoverAsync(string topic)
        {
            //it may still be cached as existing
            await KafkaCommon.ForgetTopic(commonHost, topic);

            await KafkaCommon.EnsureTopic(commonHost, topic);

            //the producer caches the topic as missing until its own handle refreshes the metadata
            using (var producerAdmin = new DependentAdminClientBuilder(producer.Handle).Build())
                _ = producerAdmin.GetMetadata(topic, TimeSpan.FromSeconds(10));
        }

        private async Task AckListeningThread()
        {
            //The topic is only this producer's and has one partition, so it's assigned directly instead of subscribed. Subscribing joins a new
            //group, which waits for the broker's group.initial.rebalance.delay.ms, 3 seconds by default, and held up the first acknowledgement.
            //Confluent requires a group ID, but an assigned consumer that never commits doesn't join or create the group.
            var consumerConfig = new ConsumerConfig();
            consumerConfig.BootstrapServers = commonHost.Host;
            consumerConfig.GroupId = ackTopic;
            consumerConfig.EnableAutoCommit = false;
            if (commonHost.UserName is not null && commonHost.Password is not null)
            {
                consumerConfig.SecurityProtocol = commonHost.UseTls ? SecurityProtocol.SaslSsl : SecurityProtocol.SaslPlaintext;
                consumerConfig.SaslMechanism = SaslMechanism.Plain;
                consumerConfig.SaslUsername = commonHost.UserName;
                consumerConfig.SaslPassword = commonHost.Password;
            }
            else if (commonHost.UseTls)
            {
                consumerConfig.SecurityProtocol = SecurityProtocol.Ssl;
            }

            //the retry stays inside the outer try, a goto out of it would run the finally and dispose the canceller on a transient error
            try
            {
            retry:

                try
                {
                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                    {
                        //the topic is new, so reading from the start catches acknowledgements sent before the assignment
                        consumer.Assign(new TopicPartitionOffset(ackTopic, 0, Offset.Beginning));
                        try
                        {
                            for (; ; )
                            {
                                var consumerResult = consumer.Consume(canceller.Token);

                                if (!ackCallbacks.TryRemove(consumerResult.Message.Key, out var waiter))
                                    continue;

                                Acknowledgement? acknowledgement = null;
                                try
                                {
                                    var response = consumerResult.Message.Value;
                                    if (encryptor is not null)
                                        response = encryptor.Decrypt(response);
                                    if (compressor is not null)
                                        response = compressor.Decompress(response);
                                    acknowledgement = serializer.Deserialize<Acknowledgement>(response);
                                    acknowledgement ??= new Acknowledgement(serializer, "Invalid Acknowledgement");
                                }
                                catch (Exception ex)
                                {
                                    acknowledgement = new Acknowledgement(serializer, ex.Message);
                                }

                                _ = waiter.TrySetResult(acknowledgement);

                                if (canceller.IsCancellationRequested)
                                    break;
                            }
                        }
                        finally
                        {
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
                    await KafkaCommon.DeleteTopic(commonHost, ackTopic);
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
        /// Cancels the acknowledgement listener and waits for it to delete the acknowledgement topic, disposes the Kafka producer, and releases semaphore resources.
        /// </remarks>
        public void Dispose()
        {
            canceller.Cancel();
            if (ackListening is not null)
            {
                try
                {
                    ackListening.Wait();
                }
                catch (AggregateException ex)
                {
                    log?.Error(ex.InnerException ?? ex);
                }
            }
            producer.Dispose();
            listenerStartedLock.Dispose();
            canceller.Dispose();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="KafkaProducer"/>.
        /// </summary>
        /// <remarks>
        /// Cancels the acknowledgement listener and waits for it to delete the acknowledgement topic, disposes the Kafka producer, and releases semaphore resources.
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            canceller.Cancel();
            if (ackListening is not null)
            {
                try
                {
                    await ackListening;
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
            }
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

            //Started now so the first command sent with DispatchAwait doesn't wait for the acknowledgement topic to be created and assigned.
            //If this fails the error is logged and the first send that needs an acknowledgement tries again.
            if (!listenerStarted)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await listenerStartedLock.WaitAsync(canceller.Token);
                        try
                        {
                            if (!listenerStarted)
                            {
                                await KafkaCommon.CreateTopic(commonHost, ackTopic);
                                ackListening = Task.Run(AckListeningThread);
                                listenerStarted = true;
                            }
                        }
                        finally
                        {
                            _ = listenerStartedLock.Release();
                        }
                    }
                    catch (Exception ex)
                    {
                        //disposing the producer before it started isn't an error
                        if (!canceller.IsCancellationRequested)
                            log?.Error(ex);
                    }
                });
            }
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
