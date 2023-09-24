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
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed class AzureServiceBusProducer : ICommandProducer, IEventProducer, IAsyncDisposable
    {
        private static readonly string applicationName = Config.EntryAssemblyName;

        private bool listenerStarted = false;
        private SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly string host;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;
        private readonly string ackTopic;
        private readonly string ackSubscription;
        private ConcurrentDictionary<Type, string> commandTypes;
        private ConcurrentDictionary<Type, string> eventTypes;
        private readonly ServiceBusClient client;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public AzureServiceBusProducer(string host, SymmetricConfig symmetricConfig, string environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;

            this.ackTopic = $"ACK-{Guid.NewGuid().ToString("N")}";
            this.ackSubscription = applicationName.Truncate(AzureServiceBusCommon.SubscriptionMaxLength);

            this.commandTypes = new();
            this.eventTypes = new();
            this.client = new ServiceBusClient(host);

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
        }

        public string ConnectionString => host;

        Task ICommandProducer.DispatchAsync(ICommand command, string source) { return SendAsync(command, false, source); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command, string source) { return SendAsync(command, true, source); }
        Task IEventProducer.DispatchAsync(IEvent @event, string source) { return SendAsync(@event, source); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement, string source)
        {
            var commandType = command.GetType();
            if (!commandTypes.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");

            if (!String.IsNullOrWhiteSpace(environment))
                topic = $"{environment}_{topic}".Truncate(AzureServiceBusCommon.TopicMaxLength);
            else
                topic = topic.Truncate(AzureServiceBusCommon.TopicMaxLength);

            if (requireAcknowledgement)
            {
                await listenerStartedLock.WaitAsync();
                try
                {
                    if (!listenerStarted)
                    {
                        await AzureServiceBusCommon.EnsureTopic(host, ackTopic, true);
                        await AzureServiceBusCommon.EnsureSubscription(host, ackTopic, ackSubscription, true);

                        _ = AckListeningThread();
                        listenerStarted = true;
                    }
                }
                finally
                {
                    _ = listenerStartedLock.Release();
                }
            }

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureServiceBusCommandMessage()
            {
                Message = command,
                Claims = claims,
                Source = source
            };

            var body = AzureServiceBusCommon.Serialize(message);
            if (symmetricConfig != null)
                body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

            if (requireAcknowledgement)
            {
                var ackKey = Guid.NewGuid().ToString("N");

                var waiter = new SemaphoreSlim(0, 1);

                try
                {

                    Acknowledgement ack = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        ack = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var serviceBusMessage = new ServiceBusMessage(body);
                    serviceBusMessage.ReplyTo = ackTopic;
                    serviceBusMessage.ReplyToSessionId = ackKey;
                    await using (var sender = client.CreateSender(topic))
                    {
                        await sender.SendMessageAsync(serviceBusMessage);
                    }

                    await waiter.WaitAsync();

                    if (!ack.Success)
                        throw new AcknowledgementException(ack, topic);
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
                await using (var sender = client.CreateSender(topic))
                {
                    await sender.SendMessageAsync(serviceBusMessage);
                }
            }
        }

        private async Task SendAsync(IEvent @event, string source)
        {
            var eventType = @event.GetType();
            if (!eventTypes.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(AzureServiceBusProducer)}");

            if (!String.IsNullOrWhiteSpace(environment))
                topic = $"{environment}_{topic}".Truncate(AzureServiceBusCommon.TopicMaxLength);
            else
                topic = topic.Truncate(AzureServiceBusCommon.TopicMaxLength);

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureServiceBusEventMessage()
            {
                Message = @event,
                Claims = claims,
                Source = source,
            };

            var body = AzureServiceBusCommon.Serialize(message);
            if (symmetricConfig != null)
                body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

            var serviceBusMessage = new ServiceBusMessage(body);
            await using (var sender = client.CreateSender(topic))
            {
                await sender.SendMessageAsync(serviceBusMessage);
            }
        }

        private async Task AckListeningThread()
        {
        retry:

            try
            {
                await using (var receiver = client.CreateReceiver(ackTopic, ackSubscription))
                {
                    for (; ; )
                    {
                        var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                        if (serviceBusMessage == null)
                            continue;
                        await receiver.CompleteMessageAsync(serviceBusMessage);

                        if (!ackCallbacks.TryRemove(serviceBusMessage.SessionId, out var callback))
                            continue;

                        Acknowledgement ack = null;
                        try
                        {
                            var response = serviceBusMessage.Body.ToStream();
                            if (symmetricConfig != null)
                                response = SymmetricEncryptor.Decrypt(symmetricConfig, response, false);
                            ack = await AzureServiceBusCommon.DeserializeAsync<Acknowledgement>(response);
                        }
                        catch (Exception ex)
                        {
                            ack = new Acknowledgement()
                            {
                                Success = false,
                                ErrorMessage = ex.Message
                            };
                        }

                        callback(ack);

                        if (canceller.IsCancellationRequested)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(ex);
                if (!canceller.IsCancellationRequested)
                {
                    await Task.Delay(AzureServiceBusCommon.RetryDelay);
                    goto retry;
                }
            }
            finally
            {
                try
                {
                    await AzureServiceBusCommon.DeleteTopic(host, ackTopic);
                    await AzureServiceBusCommon.DeleteSubscription(host, ackTopic, ackSubscription);
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
            await client.DisposeAsync();
            listenerStartedLock.Dispose();
        }

        void ICommandProducer.RegisterCommandType(Type type)
        {
            if (commandTypes.ContainsKey(type))
                return;
            var topic = Bus.GetCommandTopic(type);
            commandTypes.TryAdd(type, topic);
        }
        IEnumerable<Type> ICommandProducer.GetCommandTypes()
        {
            return commandTypes.Keys;
        }

        void IEventProducer.RegisterEventType(Type type)
        {
            if (eventTypes.ContainsKey(type))
                return;
            var topic = Bus.GetEventTopic(type);
            eventTypes.TryAdd(type, topic);
        }
        IEnumerable<Type> IEventProducer.GetEventTypes()
        {
            return eventTypes.Keys;
        }
    }
}
