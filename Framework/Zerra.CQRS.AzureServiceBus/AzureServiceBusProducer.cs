// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Security.Claims;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureServiceBus
{
    /// <summary>
    /// Azure Service Bus implementation of command and event producer for distributed CQRS messaging.
    /// </summary>
    /// <remarks>
    /// Provides high-performance, reliable message delivery to Azure Service Bus queues and topics.
    /// Supports command acknowledgements with automatic retry logic and optional message encryption.
    /// Thread-safe for concurrent operations.
    /// </remarks>
    public sealed class AzureServiceBusProducer : ICommandProducer, IEventProducer, IAsyncDisposable
    {
        private bool listenerStarted = false;
        private readonly SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly string host;
        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ILogger? log;
        private readonly string? environment;
        private readonly string ackQueue;
        private readonly ConcurrentDictionary<Type, string> queueByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByQueueOrTopic;
        private readonly ServiceBusClient client;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusProducer"/> class.
        /// </summary>
        /// <param name="host">The Azure Service Bus connection string.</param>
        /// <param name="serializer">The serializer for message serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="log">Optional logger for diagnostic information.</param>
        /// <param name="environment">Optional environment name to prefix queue and topic names for isolation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public AzureServiceBusProducer(string host, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.log = log;
            this.environment = environment;

            this.ackQueue = $"ACK-{Guid.NewGuid():N}";

            this.queueByCommandType = new();
            this.topicByEventType = new();
            this.throttleByQueueOrTopic = new();
            this.client = new ServiceBusClient(host);

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
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
            if (!queueByCommandType.TryGetValue(commandType, out var queue))
                throw new Exception($"{commandType.Name} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(queue, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(AzureServiceBusProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    queue = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, queue);
                else
                    queue = queue.Truncate(AzureServiceBusCommon.EntityNameMaxLength);

                if (requireAcknowledgement)
                {
                    if (!listenerStarted)
                    {
                        await listenerStartedLock.WaitAsync();
                        try
                        {
                            if (!listenerStarted)
                            {
                                await AzureServiceBusCommon.EnsureQueue(host, ackQueue, true);

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

                var message = new AzureServiceBusMessage()
                {
                    MessageData = serializer.SerializeBytes(command),
                    MessageType = command.GetType(),
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

                    var waiter = new SemaphoreSlim(0, 1);

                    try
                    {
                        Acknowledgement? acknowledgement = null;
                        _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                        {
                            acknowledgement = ackFromCallback;
                            _ = waiter.Release();
                        });

                        var serviceBusMessage = new ServiceBusMessage(body);
                        serviceBusMessage.ReplyTo = ackQueue;
                        serviceBusMessage.ReplyToSessionId = ackKey;
                        await using (var sender = client.CreateSender(queue))
                        {
                            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                        }

                        await waiter.WaitAsync(cancellationToken);

                        Acknowledgement.ThrowIfFailed(acknowledgement);
                    }
                    finally
                    {
                        _ = ackCallbacks.TryRemove(ackKey, out _);
                        waiter.Dispose();
                    }
                }
                else
                {
                    var serviceBusMessage = new ServiceBusMessage(body);
                    await using (var sender = client.CreateSender(queue))
                    {
                        await sender.SendMessageAsync(serviceBusMessage);
                    }
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
            if (!queueByCommandType.TryGetValue(commandType, out var queue))
                throw new Exception($"{commandType.Name} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(queue, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(AzureServiceBusProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    queue = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, queue);
                else
                    queue = queue.Truncate(AzureServiceBusCommon.EntityNameMaxLength);

                if (!listenerStarted)
                {
                    await listenerStartedLock.WaitAsync();
                    try
                    {
                        if (!listenerStarted)
                        {
                            await AzureServiceBusCommon.EnsureQueue(host, ackQueue, true);

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

                var message = new AzureServiceBusMessage()
                {
                    MessageData = serializer.SerializeBytes(command),
                    MessageType = command.GetType(),
                    HasResult = true,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var ackKey = Guid.NewGuid().ToString("N");

                var waiter = new SemaphoreSlim(0, 1);

                try
                {
                    Acknowledgement? acknowledgement = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        acknowledgement = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var serviceBusMessage = new ServiceBusMessage(body);
                    serviceBusMessage.ReplyTo = ackQueue;
                    serviceBusMessage.ReplyToSessionId = ackKey;
                    await using (var sender = client.CreateSender(queue))
                    {
                        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                    }

                    await waiter.WaitAsync(cancellationToken);

                    var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(acknowledgement)!;

                    return result;
                }
                finally
                {
                    _ = ackCallbacks.TryRemove(ackKey, out _);
                    waiter.Dispose();
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
            if (!topicByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.Name} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.Name} is not registered with {nameof(AzureServiceBusProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(AzureServiceBusCommon.EntityNameMaxLength);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new AzureServiceBusMessage()
                {
                    MessageData = serializer.SerializeBytes(@event),
                    MessageType = @event.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var serviceBusMessage = new ServiceBusMessage(body);
                await using (var sender = client.CreateSender(topic))
                {
                    await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                }
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        private async Task AckListeningThread()
        {
        retry:

            try
            {
                await using (var receiver = client.CreateReceiver(ackQueue))
                {
                    for (; ; )
                    {
                        var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                        if (serviceBusMessage is null)
                            continue;
                        await receiver.CompleteMessageAsync(serviceBusMessage);

                        if (!ackCallbacks.TryRemove(serviceBusMessage.SessionId, out var callback))
                            continue;

                        Acknowledgement? acknowledgement = null;
                        try
                        {
                            var response = serviceBusMessage.Body.ToStream();
                            if (encryptor is not null)
                                response = encryptor.Decrypt(response, false);
                            acknowledgement = await serializer.DeserializeAsync<Acknowledgement>(response, canceller.Token);
                            acknowledgement ??= new Acknowledgement("Invalid Acknowledgement");
                        }
                        catch (Exception ex)
                        {
                            acknowledgement = new Acknowledgement(ex.Message);
                        }

                        callback(acknowledgement);

                        if (canceller.IsCancellationRequested)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Error(ex);
                if (!canceller.IsCancellationRequested)
                {
                    await Task.Delay(AzureServiceBusCommon.RetryDelay);
                    goto retry;
                }
            }
            finally
            {
                listenerStarted = false;

                try
                {
                    await AzureServiceBusCommon.DeleteTopic(host, ackQueue);
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
                canceller.Dispose();
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="AzureServiceBusProducer"/>.
        /// </summary>
        /// <remarks>
        /// Cancels acknowledgement listening, closes the Service Bus client connection,
        /// and releases semaphore resources.
        /// </remarks>
        public async ValueTask DisposeAsync()
        {
            canceller.Cancel();
            await client.DisposeAsync();
            listenerStartedLock.Dispose();
        }

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (queueByCommandType.ContainsKey(type))
                return;
            _ = queueByCommandType.TryAdd(type, topic);
            if (throttleByQueueOrTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByQueueOrTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicByEventType.ContainsKey(type))
                return;
            _ = topicByEventType.TryAdd(type, topic);
            if (throttleByQueueOrTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByQueueOrTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
    }
}
