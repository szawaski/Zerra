// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Security.Claims;
using Zerra.CQRS.Network;
using Zerra.Compression;
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
    public sealed class AzureServiceBusProducer : ICommandProducer, IEventProducer, IDisposable, IAsyncDisposable
    {
        private bool listenerStarted = false;
        private Task? ackListening = null;
        private readonly SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly ISerializer serializer;
        private readonly IEncryptor? encryptor;
        private readonly ICompressor? compressor;
        private readonly ILogger? log;
        private readonly string? environment;
        private readonly string ackQueue;
        private readonly AzureServiceBusCommonNamespace commonNamespace;
        private readonly ConcurrentDictionary<Type, string> queueByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByQueueOrTopic;
        private readonly ConcurrentDictionary<string, ServiceBusSender> senderByQueueOrTopic;
        private readonly ConcurrentDictionary<string, Lazy<Task>> recoveryByQueueOrTopic;
        private readonly ServiceBusClient client;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Acknowledgement>> ackCallbacks;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBusProducer"/> class.
        /// </summary>
        /// <param name="host">The Azure Service Bus connection string.</param>
        /// <param name="serializer">The serializer for message serialization and deserialization.</param>
        /// <param name="encryptor">Optional encryptor for message encryption. If null, messages are not encrypted.</param>
        /// <param name="compressor">Optional compressor for message compression, applied before encryption. If null, messages are not compressed.</param>
        /// <param name="log">Optional logger for diagnostic information.</param>
        /// <param name="environment">Optional environment name to prefix queue and topic names for isolation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null or empty.</exception>
        public AzureServiceBusProducer(string host, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.commonNamespace = AzureServiceBusCommon.GetNamespace(host);
            this.serializer = serializer;
            this.encryptor = encryptor;
            this.compressor = compressor;
            this.log = log;
            this.environment = environment;

            this.ackQueue = $"ACK-{Guid.NewGuid():N}";

            this.queueByCommandType = new();
            this.topicByEventType = new();
            this.throttleByQueueOrTopic = new();
            this.senderByQueueOrTopic = new();
            this.recoveryByQueueOrTopic = new();
            this.client = new ServiceBusClient(host);

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
            if (!queueByCommandType.TryGetValue(commandType, out var queue))
                throw new Exception($"{commandType.Name} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(queue, out var throttle))
                throw new Exception($"{commandType.Name} is not registered with {nameof(AzureServiceBusProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                if (requireAcknowledgement)
                {
                    if (!listenerStarted)
                    {
                        await listenerStartedLock.WaitAsync();
                        try
                        {
                            if (!listenerStarted)
                            {
                                await AzureServiceBusCommon.CreateQueue(commonNamespace, ackQueue, true);

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

                var message = new AzureServiceBusMessage()
                {
                    MessageData = serializer.SerializeBytes(command, command.GetType()),
                    MessageType = commandType.AssemblyQualifiedName,
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

                    var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                    try
                    {
                        _ = ackCallbacks.TryAdd(ackKey, waiter);

                        var serviceBusMessage = new ServiceBusMessage(body);
                        serviceBusMessage.ReplyTo = ackQueue;
                        serviceBusMessage.ReplyToSessionId = ackKey;
                        await SendMessageAsync(queue, false, serviceBusMessage, cancellationToken);

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
                    var serviceBusMessage = new ServiceBusMessage(body);
                    await SendMessageAsync(queue, false, serviceBusMessage, cancellationToken);
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
                if (!listenerStarted)
                {
                    await listenerStartedLock.WaitAsync();
                    try
                    {
                        if (!listenerStarted)
                        {
                            await AzureServiceBusCommon.CreateQueue(commonNamespace, ackQueue, true);

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

                var message = new AzureServiceBusMessage()
                {
                    MessageData = serializer.SerializeBytes(command, command.GetType()),
                    MessageType = commandType.AssemblyQualifiedName,
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

                var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                try
                {
                    _ = ackCallbacks.TryAdd(ackKey, waiter);

                    var serviceBusMessage = new ServiceBusMessage(body);
                    serviceBusMessage.ReplyTo = ackQueue;
                    serviceBusMessage.ReplyToSessionId = ackKey;
                    await SendMessageAsync(queue, false, serviceBusMessage, cancellationToken);

                    Acknowledgement acknowledgement;
                    using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                        acknowledgement = await waiter.Task;

                    var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(commandType.Name, serializer, acknowledgement)!;

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
            if (!topicByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.Name} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.Name} is not registered with {nameof(AzureServiceBusProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new AzureServiceBusMessage()
                {
                    MessageData = serializer.SerializeBytes(@event, @event.GetType()),
                    MessageType = eventType.AssemblyQualifiedName,
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = serializer.SerializeBytes(message);
                if (compressor is not null)
                    body = compressor.Compress(body);
                if (encryptor is not null)
                    body = encryptor.Encrypt(body);

                var serviceBusMessage = new ServiceBusMessage(body);
                await SendMessageAsync(topic, true, serviceBusMessage, cancellationToken);
            }
            finally
            {
                _ = throttle.Release();
            }
        }

        //queues and topics aren't ensured up front for faster startup, only after a send fails
        private async Task SendMessageAsync(string queueOrTopic, bool isTopic, ServiceBusMessage serviceBusMessage, CancellationToken cancellationToken)
        {
            var sender = senderByQueueOrTopic[queueOrTopic];
            bool notFound;
            try
            {
                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
                return;
            }
            catch (ServiceBusException ex) when (!ex.IsTransient)
            {
                log?.Warn($"{nameof(AzureServiceBusProducer)} failed to send to {queueOrTopic}, ensuring it and trying again: {ex.Reason} {ex.Message}");
                notFound = ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound;
            }

            //sends failing at the same time share one recovery
            var recovery = recoveryByQueueOrTopic.GetOrAdd(queueOrTopic, _ => new Lazy<Task>(() => RecoverAsync(queueOrTopic, isTopic, notFound)));
            try
            {
                await recovery.Value;
            }
            finally
            {
                _ = ((ICollection<KeyValuePair<string, Lazy<Task>>>)recoveryByQueueOrTopic).Remove(new(queueOrTopic, recovery));
            }

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
        }

        private async Task RecoverAsync(string queueOrTopic, bool isTopic, bool notFound)
        {
            //it may still be cached as existing
            if (notFound)
                await AzureServiceBusCommon.Forget(commonNamespace, queueOrTopic);

            if (isTopic)
                await AzureServiceBusCommon.EnsureTopic(commonNamespace, queueOrTopic, false);
            else
                await AzureServiceBusCommon.EnsureQueue(commonNamespace, queueOrTopic, false);
        }

        private async Task AckListeningThread()
        {
            //the retry stays inside the outer try, a goto out of it would run the finally and dispose the canceller on a transient error
            try
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

                            if (!ackCallbacks.TryRemove(serviceBusMessage.SessionId, out var waiter))
                                continue;

                            Acknowledgement? acknowledgement = null;
                            try
                            {
                                var response = serviceBusMessage.Body.ToStream();
                                if (encryptor is not null)
                                    response = encryptor.Decrypt(response, false);
                                if (compressor is not null)
                                    response = compressor.Decompress(response, false);
                                acknowledgement = await serializer.DeserializeAsync<Acknowledgement>(response, canceller.Token);
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
                }
                catch (Exception ex)
                {
                    //disposing cancels the receive, that isn't an error
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(ex);
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }
            }
            finally
            {
                listenerStarted = false;

                try
                {
                    await AzureServiceBusCommon.DeleteQueue(commonNamespace, ackQueue);
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
                canceller.Dispose();
            }
        }

        /// <inheritdoc />
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
            await client.DisposeAsync();
            listenerStartedLock.Dispose();
            canceller.Dispose();
        }

        /// <inheritdoc />
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
            _ = client.DisposeAsync().AsTask();
            listenerStartedLock.Dispose();
            canceller.Dispose();
        }


        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (queueByCommandType.ContainsKey(type))
                return;
            topic = BuildEntityName(topic, "command queue");
            _ = queueByCommandType.TryAdd(type, topic);
            if (throttleByQueueOrTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByQueueOrTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            _ = senderByQueueOrTopic.TryAdd(topic, client.CreateSender(topic));

            //Started now so the first command sent with DispatchAwait doesn't wait for the acknowledgement queue to be created and received from.
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
                                await AzureServiceBusCommon.CreateQueue(commonNamespace, ackQueue, true);

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
            if (topicByEventType.ContainsKey(type))
                return;
            topic = BuildEntityName(topic, "event topic");
            _ = topicByEventType.TryAdd(type, topic);
            if (throttleByQueueOrTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByQueueOrTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            _ = senderByQueueOrTopic.TryAdd(topic, client.CreateSender(topic));
        }

        private string BuildEntityName(string topic, string kind)
        {
            bool truncated;
            if (!String.IsNullOrWhiteSpace(environment))
                topic = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, topic, out truncated);
            else
                topic = topic.Truncate(AzureServiceBusCommon.EntityNameMaxLength, out truncated);
            if (truncated)
                log?.Warn($"{nameof(AzureServiceBusProducer)} truncated the {kind} to {AzureServiceBusCommon.EntityNameMaxLength} characters: {topic}. Another entity truncating to the same name would receive these messages.");
            return topic;
        }
    }
}
