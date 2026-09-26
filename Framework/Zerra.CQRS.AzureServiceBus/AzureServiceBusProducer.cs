// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed class AzureServiceBusProducer : ICommandProducer, IEventProducer, IDisposable, IAsyncDisposable
    {
        private bool listenerStarted = false;
        private Task? ackListening = null;
        private readonly SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly AzureServiceBusCommonNamespace commonNamespace;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly string? environment;
        private readonly string ackQueue;
        private readonly ConcurrentDictionary<Type, string> queueByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByQueueOrTopic;
        //one sender per queue or topic for the producer's life, a sender opens its link to the broker on first send and keeps it
        private readonly ConcurrentDictionary<string, ServiceBusSender> senderByQueueOrTopic;
        private readonly ConcurrentDictionary<string, Lazy<Task>> recoveryByQueueOrTopic;
        private readonly ServiceBusClient client;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Acknowledgement>> ackCallbacks;
        public AzureServiceBusProducer(string host, SymmetricConfig? symmetricConfig, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.commonNamespace = AzureServiceBusCommon.GetNamespace(host);
            this.symmetricConfig = symmetricConfig;
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
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(queue, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");

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
                    MessageData = AzureServiceBusCommon.Serialize(command),
                    MessageType = command.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = AzureServiceBusCommon.Serialize(message);
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                if (requireAcknowledgement)
                {
                    var ackKey = Guid.NewGuid().ToString("N");

                    //completed by the acknowledgement listener, continuations run off its thread so it goes back to reading
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

                        Acknowledgement.ThrowIfFailed(acknowledgement);
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
                throttle.Release();
            }
        }

        private async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!queueByCommandType.TryGetValue(commandType, out var queue))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(queue, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");

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
                    MessageData = AzureServiceBusCommon.Serialize(command),
                    MessageType = command.GetType(),
                    HasResult = true,
                    Claims = claims,
                    Source = source
                };

                var body = AzureServiceBusCommon.Serialize(message);
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                var ackKey = Guid.NewGuid().ToString("N");

                //completed by the acknowledgement listener, continuations run off its thread so it goes back to reading
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

                    var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(acknowledgement)!;

                    return result;
                }
                finally
                {
                    _ = ackCallbacks.TryRemove(ackKey, out _);
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task SendAsync(IEvent @event, string source, CancellationToken cancellationToken)
        {
            var eventType = @event.GetType();
            if (!topicByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");
            if (!throttleByQueueOrTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new AzureServiceBusMessage()
                {
                    MessageData = AzureServiceBusCommon.Serialize(@event),
                    MessageType = @event.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = AzureServiceBusCommon.Serialize(message);
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                var serviceBusMessage = new ServiceBusMessage(body);
                await SendMessageAsync(topic, true, serviceBusMessage, cancellationToken);
            }
            finally
            {
                throttle.Release();
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
                _ = Log.WarnAsync($"{nameof(AzureServiceBusProducer)} failed to send to {queueOrTopic}, ensuring it and trying again: {ex.Reason} {ex.Message}");
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
                                if (symmetricConfig is not null)
                                    response = SymmetricEncryptor.Decrypt(symmetricConfig, response, false);
                                acknowledgement = await AzureServiceBusCommon.DeserializeAsync<Acknowledgement>(response);
                                acknowledgement ??= new Acknowledgement("Invalid Acknowledgement");
                            }
                            catch (Exception ex)
                            {
                                acknowledgement = new Acknowledgement(ex.Message);
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
                        _ = Log.ErrorAsync(ex);
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
                    _ = Log.ErrorAsync(ex);
                }
                canceller.Dispose();
            }
        }

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
                    _ = Log.ErrorAsync(ex);
                }
            }
            await client.DisposeAsync();
            listenerStartedLock.Dispose();
            canceller.Dispose();
        }

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
                    _ = Log.ErrorAsync(ex.InnerException ?? ex);
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
            topic = BuildEntityName(topic);
            queueByCommandType.TryAdd(type, topic);
            if (throttleByQueueOrTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByQueueOrTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            //created with the queue or topic, the client closes it on dispose
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
                            _ = Log.ErrorAsync(ex);
                    }
                });
            }
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicByEventType.ContainsKey(type))
                return;
            topic = BuildEntityName(topic);
            topicByEventType.TryAdd(type, topic);
            if (throttleByQueueOrTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByQueueOrTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            //created with the queue or topic, the client closes it on dispose
            _ = senderByQueueOrTopic.TryAdd(topic, client.CreateSender(topic));
        }

        private string BuildEntityName(string topic)
        {
            if (!String.IsNullOrWhiteSpace(environment))
                return StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, topic);
            else
                return topic.Truncate(AzureServiceBusCommon.EntityNameMaxLength);
        }
    }
}
