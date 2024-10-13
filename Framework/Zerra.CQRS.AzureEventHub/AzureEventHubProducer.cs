// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AzureEventHubProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private readonly object locker = new();

        private bool listenerStarted = false;

        private readonly string host;
        private readonly string eventHubName;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        private readonly string? environment;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        public AzureEventHubProducer(string host, string eventHubName, SymmetricConfig? symmetricConfig, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));
            if (String.IsNullOrWhiteSpace(eventHubName)) throw new ArgumentNullException(nameof(eventHubName));

            this.host = host;
            this.eventHubName = eventHubName;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;

            this.canceller = new();
            this.ackCallbacks = new();
            this.environment = environment;
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();
        }

        public string ConnectionString => host;

        Task ICommandProducer.DispatchAsync(ICommand command, string source) { return SendAsync(command, false, source); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command, string source) { return SendAsync(command, true, source); }
        Task<TResult?> ICommandProducer.DispatchAsyncAwait<TResult>(ICommand<TResult> command, string source) where TResult : default { return SendAsync(command, source); }
        Task IEventProducer.DispatchAsync(IEvent @event, string source) { return SendAsync(@event, source); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement, string source)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureEventHubProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureEventHubProducer)}");

            await throttle.WaitAsync();

            try
            {
                if (requireAcknowledgement)
                {
                    if (!listenerStarted)
                    {
                        lock (locker)
                        {
                            if (!listenerStarted)
                            {
                                _ = AckListeningThread();
                                listenerStarted = true;
                            }
                        }
                    }
                }

                string? ackKey = null;
                var type = command.GetType().GetNiceName();

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new AzureEventHubMessage()
                {
                    MessageData = AzureEventHubCommon.Serialize(command),
                    MessageType = command.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = AzureEventHubCommon.Serialize(message);
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                await using (var producer = new EventHubProducerClient(host, eventHubName))
                {
                    if (requireAcknowledgement)
                        ackKey = Guid.NewGuid().ToString("N");

                    var eventData = new EventData(body);
                    eventData.Properties[AzureEventHubCommon.TypeProperty] = type;
                    if (!String.IsNullOrWhiteSpace(environment))
                        eventData.Properties[AzureEventHubCommon.EnvironmentProperty] = environment;
                    if (requireAcknowledgement)
                        eventData.Properties[AzureEventHubCommon.AckProperty] = ackKey;

                    if (requireAcknowledgement)
                    {
                        var waiter = new SemaphoreSlim(0, 1);

                        try
                        {
                            Acknowledgement? acknowledgement = null;
                            _ = ackCallbacks.TryAdd(ackKey!, (ackFromCallback) =>
                            {
                                acknowledgement = ackFromCallback;
                                _ = waiter.Release();
                            });

                            await producer.SendAsync(new EventData[] { eventData });

                            await waiter.WaitAsync();

                            Acknowledgement.ThrowIfFailed(acknowledgement);
                        }
                        finally
                        {
                            waiter.Dispose();
                        }
                    }
                    else
                    {
                        await producer.SendAsync(new EventData[] { eventData });
                    }
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task<TResult?> SendAsync<TResult>(ICommand<TResult> command, string source)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureEventHubProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(AzureEventHubProducer)}");

            await throttle.WaitAsync();

            try
            {
                if (!listenerStarted)
                {
                    lock (locker)
                    {
                        if (!listenerStarted)
                        {
                            _ = AckListeningThread();
                            listenerStarted = true;
                        }
                    }
                }

                string? ackKey = null;
                var type = command.GetType().GetNiceName();

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new AzureEventHubMessage()
                {
                    MessageData = AzureEventHubCommon.Serialize(command),
                    MessageType = command.GetType(),
                    HasResult = true,
                    Claims = claims,
                    Source = source
                };

                var body = AzureEventHubCommon.Serialize(message);
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                await using (var producer = new EventHubProducerClient(host, eventHubName))
                {
                    ackKey = Guid.NewGuid().ToString("N");

                    var eventData = new EventData(body);
                    eventData.Properties[AzureEventHubCommon.TypeProperty] = type;
                    if (!String.IsNullOrWhiteSpace(environment))
                        eventData.Properties[AzureEventHubCommon.EnvironmentProperty] = environment;
                    eventData.Properties[AzureEventHubCommon.AckProperty] = ackKey;

                    var waiter = new SemaphoreSlim(0, 1);

                    try
                    {
                        Acknowledgement? acknowledgement = null;
                        _ = ackCallbacks.TryAdd(ackKey!, (ackFromCallback) =>
                        {
                            acknowledgement = ackFromCallback;
                            _ = waiter.Release();
                        });

                        await producer.SendAsync(new EventData[] { eventData });

                        await waiter.WaitAsync();

                        var result = (TResult?)Acknowledgement.GetResultOrThrowIfFailed(acknowledgement);

                        return result;
                    }
                    finally
                    {
                        waiter.Dispose();
                    }
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
                throw new Exception($"{eventType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {this.GetType().GetNiceName()}");

            await throttle.WaitAsync();

            try
            {
                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new AzureEventHubMessage()
                {
                    MessageData = AzureEventHubCommon.Serialize(@event),
                    MessageType = @event.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = AzureEventHubCommon.Serialize(message);
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                await using (var producer = new EventHubProducerClient(host, eventHubName))
                {
                    using (var eventBatch = await producer.CreateBatchAsync())
                    {
                        var eventData = new EventData(body);
                        if (!String.IsNullOrWhiteSpace(environment))
                            eventData.Properties[AzureEventHubCommon.EnvironmentProperty] = environment;

                        await producer.SendAsync(new EventData[] { eventData });
                    }

                    await producer.CloseAsync();
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task AckListeningThread()
        {
        retry:

            try
            {
                await using (var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, host, eventHubName))
                {
                    await foreach (var partitionEvent in consumer.ReadEventsAsync(canceller.Token))
                    {
                        if (!partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckProperty, out var ackObject))
                            continue;
                        if (ackObject is not string ackString)
                            continue;
                        if (ackString != Boolean.TrueString)
                            continue;

                        string? ackKey = null;
                        if (!partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckKeyProperty, out var ackKeyObject))
                            continue;
                        if (ackKeyObject is not string ackKeyString)
                            continue;
                        ackKey = ackKeyString;

                        if (!ackCallbacks.TryRemove(ackKey, out var callback))
                            continue;

                        Acknowledgement? acknowledgement = null;
                        try
                        {
                            var response = partitionEvent.Data.EventBody.ToArray();
                            if (symmetricConfig is not null)
                                response = SymmetricEncryptor.Decrypt(symmetricConfig, response);
                            acknowledgement = AzureEventHubCommon.Deserialize<Acknowledgement>(response);
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
                if (!canceller.IsCancellationRequested)
                {
                    _ = Log.ErrorAsync(ex);
                    await Task.Delay(AzureEventHubCommon.RetryDelay);
                    goto retry;
                }
            }
            finally
            {
                listenerStarted = false;
                canceller.Dispose();
            }
        }

        public void Dispose()
        {
            canceller.Cancel();
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
