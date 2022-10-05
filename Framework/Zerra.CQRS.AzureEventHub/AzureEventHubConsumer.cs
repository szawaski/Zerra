// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.Threading;

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private static readonly string requestedConsumerGroup = Config.EntryAssemblyName;

        private readonly string host;
        private readonly string eventHubName;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;

        private readonly ConcurrentList<Type> commandTypes;
        private readonly ConcurrentList<Type> eventTypes;

        private readonly TaskThrottler throttler;

        private Func<ICommand, Task> commandHandlerAsync = null;
        private Func<ICommand, Task> commandHandlerAwaitAsync = null;
        private Func<IEvent, Task> eventHandlerAsync = null;

        private bool isOpen;
        private CancellationTokenSource canceller = null;

        public string ConnectionString => host;

        public AzureEventHubConsumer(string host, string eventHubName, SymmetricConfig symmetricConfig, string environment)
        {
            this.host = host;
            this.eventHubName = eventHubName;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;

            this.throttler = new TaskThrottler();

            this.commandTypes = new ConcurrentList<Type>();
            this.eventTypes = new ConcurrentList<Type>();
        }

        void ICommandConsumer.SetHandler(Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventConsumer.SetHandler(Func<IEvent, Task> handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Server Started Connected To {this.host}");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Server Started Connected To {this.host}");
        }

        private void Open()
        {
            if (isOpen)
                return;

            isOpen = true;
            _ = Task.Run(() => ListeningThread());
        }

        private async Task ListeningThread()
        {
            canceller = new CancellationTokenSource();

        retry:

            try
            {
                var consumerGroup = await AzureEventHubCommon.GetEnsuredConsumerGroup(requestedConsumerGroup, host, eventHubName);

                await using (var consumer = new EventHubConsumerClient(consumerGroup, host, eventHubName))
                {
                    await foreach (var partitionEvent in consumer.ReadEventsAsync(canceller.Token))
                    {
                        if (!String.IsNullOrWhiteSpace(environment))
                        {
                            if (!partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.EnvironmentProperty, out var environmentNameObject))
                                continue;
                            if (environmentNameObject is not string environmentName)
                                continue;
                            if (environmentName != environment)
                                continue;
                        }
                        if (!partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.TypeProperty, out var typeNameObject))
                            continue;
                        if (typeNameObject is not string typeName)
                            continue;

                        _ = Log.TraceAsync($"Received Await: {typeName}");

                        _ = HandleEvent(typeName, partitionEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(ex);

                if (!canceller.IsCancellationRequested)
                {
                    await Task.Delay(AzureEventHubCommon.RetryDelay);
                    goto retry;
                }
            }

            canceller.Dispose();
            isOpen = false;
        }

        private async Task HandleEvent(string typeName, PartitionEvent partitionEvent)
        {
            Exception error = null;
            Type type = null;
            string ackKey = null;
            var awaitResponse = false;
            try
            {
                type = Discovery.GetTypeFromName(typeName);
                var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                var isCommand = commandTypes.Contains(type) && typeDetail.Interfaces.Contains(typeof(ICommand));
                var isEvent = eventTypes.Contains(type) && typeDetail.Interfaces.Contains(typeof(IEvent));

                if (!isCommand && !isEvent)
                    return;

                if (partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckProperty, out var ackKeyObject))
                {
                    if (ackKeyObject is string ackKeyString)
                    {
                        ackKey = ackKeyString;
                        awaitResponse = true;
                    }
                }

                var body = partitionEvent.Data.EventBody.ToArray();
                if (symmetricConfig != null)
                    body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                var message = AzureEventHubCommon.Deserialize<AzureEventHubMessage>(body);

                if (message.Claims != null)
                {
                    var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                    Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                }

                if (isCommand)
                {
                    if (awaitResponse)
                        await commandHandlerAwaitAsync((ICommand)message.Message);
                    else
                        await commandHandlerAsync((ICommand)message.Message);
                }
                else if (isEvent)
                {
                    await eventHandlerAsync((IEvent)message.Message);
                }
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync($"Error: {type?.Name}", ex);
                error = ex;
            }

            if (awaitResponse)
            {
                try
                {
                    var ack = new Acknowledgement()
                    {
                        Success = error == null,
                        ErrorMessage = error?.Message
                    };
                    var ackBody = AzureEventHubCommon.Serialize(ack);
                    if (symmetricConfig != null)
                        ackBody = SymmetricEncryptor.Encrypt(symmetricConfig, ackBody);

                    await using (var producer = new EventHubProducerClient(host, eventHubName))
                    {
                        var eventData = new EventData(ackBody);
                        eventData.Properties[AzureEventHubCommon.AckProperty] = Boolean.TrueString;
                        eventData.Properties[AzureEventHubCommon.AckKeyProperty] = ackKey;

                        await producer.SendAsync(new EventData[] { eventData });
                    }
                }

                catch (Exception ex)
                {
                    _ = Log.ErrorAsync($"Error: {type?.Name}", ex);
                }
            }
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Server Closed On {this.host}");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Server Closed On {this.host}");
        }
        private void Close()
        {
            if (isOpen)
            {
                canceller.Cancel();
                isOpen = false;
            }
        }

        public void Dispose()
        {
            this.Close();
            commandTypes.Dispose();
            eventTypes.Dispose();
        }

        void ICommandConsumer.RegisterCommandType(Type type)
        {
            commandTypes.Add(type);
        }
        ICollection<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandTypes;
        }

        void IEventConsumer.RegisterEventType(Type type)
        {
            eventTypes.Add(type);
        }
        ICollection<Type> IEventConsumer.GetEventTypes()
        {
            return eventTypes;
        }
    }
}
