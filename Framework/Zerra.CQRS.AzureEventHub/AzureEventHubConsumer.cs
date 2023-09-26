// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AzureEventHubConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private static readonly string requestedConsumerGroup = Config.EntryAssemblyName;

        private readonly string host;
        private readonly string eventHubName;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;

        private readonly ConcurrentHashSet<Type> commandTypes;
        private readonly ConcurrentHashSet<Type> eventTypes;

        private HandleRemoteCommandDispatch commandHandlerAsync = null;
        private HandleRemoteCommandDispatch commandHandlerAwaitAsync = null;
        private HandleRemoteEventDispatch eventHandlerAsync = null;

        private bool isOpen;
        private CancellationTokenSource canceller = null;

        public string ServiceUrl => host;

        private ReceiveCounter receiveCounter = null;
        private int maxConcurrent = Environment.ProcessorCount * 8;

        public AzureEventHubConsumer(string host, string eventHubName, SymmetricConfig symmetricConfig, string environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));
            if (String.IsNullOrWhiteSpace(eventHubName)) throw new ArgumentNullException(nameof(eventHubName));

            this.host = host;
            this.eventHubName = eventHubName;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;

            this.commandTypes = new();
            this.eventTypes = new();
        }

        void ICommandConsumer.Setup(ReceiveCounter receiveCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.receiveCounter = receiveCounter;
            this.commandHandlerAsync = handlerAsync;
            this.commandHandlerAwaitAsync = handlerAwaitAsync;
        }
        void IEventConsumer.Setup(ReceiveCounter receiveCounter, HandleRemoteEventDispatch handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.receiveCounter = receiveCounter;
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Consumer Started Connected");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Consumer Started Connected");
        }

        private void Open()
        {
            if (isOpen)
                return;

            isOpen = true;
            _ = ListeningThread();
        }

        private async Task ListeningThread()
        {
            canceller = new CancellationTokenSource();
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

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

                        await throttle.WaitAsync();

                        _ = receiveCounter.BeginReceive();

                        _ = HandleEvent(throttle, typeName, partitionEvent);
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

        private async Task HandleEvent(SemaphoreSlim throttle, string typeName, PartitionEvent partitionEvent)
        {
            Exception error = null;
            Type type = null;
            string ackKey = null;
            var awaitResponse = false;

            var inHandlerContext = false;
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

                inHandlerContext = true;
                if (isCommand)
                {
                    if (awaitResponse)
                        await commandHandlerAwaitAsync((ICommand)message.Message, message.Source, false);
                    else
                        await commandHandlerAsync((ICommand)message.Message, message.Source, false);
                }
                else if (isEvent)
                {
                    await eventHandlerAsync((IEvent)message.Message, message.Source, false);
                }
                inHandlerContext = false;
            }
            catch (Exception ex)
            {
                error = ex;
                if (!inHandlerContext)
                    _ = Log.ErrorAsync(typeName, ex);
            }
            finally
            {
                if (!awaitResponse)
                    receiveCounter.CompleteReceive(throttle);
            }

            if (!awaitResponse)
                return;

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
                _ = Log.ErrorAsync(typeName, ex);
            }
            finally
            {
                receiveCounter.CompleteReceive(throttle);
            }
        }

        void ICommandConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Consumer Closed");
        }
        void IEventConsumer.Close()
        {
            Close();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Consumer Closed");
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
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (maxConcurrent < this.maxConcurrent)
                this.maxConcurrent = maxConcurrent;
            commandTypes.Add(type);
        }
        IEnumerable<Type> ICommandConsumer.GetCommandTypes()
        {
            return commandTypes;
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (maxConcurrent < this.maxConcurrent)
                this.maxConcurrent = maxConcurrent;
            eventTypes.Add(type);
        }
        IEnumerable<Type> IEventConsumer.GetEventTypes()
        {
            return eventTypes;
        }
    }
}
