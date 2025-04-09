// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AzureEventHubConsumer : ICommandConsumer, IEventConsumer, IDisposable
    {
        private static readonly string requestedConsumerGroup = Config.EntryAssemblyName ?? "Unknown_Assembly";

        private readonly string host;
        private readonly string eventHubName;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly string? environment;

        private readonly ConcurrentHashSet<Type> commandTypes;
        private readonly ConcurrentHashSet<Type> eventTypes;

        private HandleRemoteCommandDispatch? handlerAsync = null;
        private HandleRemoteCommandDispatch? handlerAwaitAsync = null;
        private HandleRemoteCommandWithResultDispatch? handlerWithResultAwaitAsync = null;
        private HandleRemoteEventDispatch? eventHandlerAsync = null;

        private bool isOpen;
        private CancellationTokenSource? canceller = null;

        private CommandCounter? commandCounter = null;
        private int? maxConcurrent = null;

        public AzureEventHubConsumer(string host, string eventHubName, SymmetricConfig? symmetricConfig, string? environment)
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

        string ICommandConsumer.MessageHost => "[Host has Secrets]";
        string IEventConsumer.MessageHost => "[Host has Secrets]";

        void ICommandConsumer.Setup(CommandCounter commandCounter, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.commandCounter = commandCounter;
            this.handlerAsync = handlerAsync;
            this.handlerAwaitAsync = handlerAwaitAsync;
            this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
        }
        void IEventConsumer.Setup(HandleRemoteEventDispatch handlerAsync)
        {
            if (isOpen)
                throw new InvalidOperationException("Connection already open");
            this.eventHandlerAsync = handlerAsync;
        }

        void ICommandConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Command Consumer Listening");
        }
        void IEventConsumer.Open()
        {
            Open();
            _ = Log.InfoAsync($"{nameof(AzureEventHubConsumer)} Event Consumer Listening");
        }

        private void Open()
        {
            if (isOpen)
                return;

            isOpen = true;

            if (maxConcurrent is null)
                throw new Exception($"{nameof(AzureEventHubConsumer)} is not setup");

            if ((commandCounter is null || handlerAsync is null || handlerAwaitAsync is null) && (eventHandlerAsync is null))
                throw new Exception($"{nameof(AzureEventHubConsumer)} is not setup");

            _ = Task.Run(ListeningThread);
        }

        private async Task ListeningThread()
        {
            canceller = new CancellationTokenSource();

        retry:

            var throttle = new SemaphoreSlim(maxConcurrent!.Value, maxConcurrent.Value);

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

                        var type = Discovery.GetTypeFromName(typeName);
                        var typeDetail = TypeAnalyzer.GetTypeDetail(type);
                        var isCommand = commandTypes.Contains(type) && typeDetail.Interfaces.Contains(typeof(ICommand));
                        var isEvent = eventTypes.Contains(type) && typeDetail.Interfaces.Contains(typeof(IEvent));

                        if (!isCommand && !isEvent)
                            return;

                        await throttle.WaitAsync(canceller.Token);

                        if (isCommand)
                        {
                            if (commandCounter is null || handlerAsync is null || handlerAwaitAsync is null || handlerWithResultAwaitAsync is null)
                                throw new Exception($"{nameof(AzureEventHubConsumer)} is not setup");

                            if (!commandCounter.BeginReceive())
                                continue; //don't receive anymore, externally will be shutdown, fill throttle
                        }
                        else if (isEvent)
                        {
                            if (eventHandlerAsync is null)
                                throw new Exception($"{nameof(AzureEventHubConsumer)} is not setup");
                        }

                        _ = Task.Run(() => HandleEvent(throttle, typeName, isCommand, isEvent, partitionEvent, canceller.Token));
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
            finally
            {
                throttle.Dispose();
                canceller.Dispose();
                isOpen = false;
            }
        }

        private async Task HandleEvent(SemaphoreSlim throttle, string typeName, bool isCommand, bool isEvent, PartitionEvent partitionEvent, CancellationToken cancellationToken)
        {
            object? result = null;
            Exception? error = null;
            string? ackKey = null;
            var awaitResponse = false;

            var inHandlerContext = false;
            try
            {
                if (partitionEvent.Data.Properties.TryGetValue(AzureEventHubCommon.AckProperty, out var ackKeyObject))
                {
                    if (ackKeyObject is string ackKeyString)
                    {
                        ackKey = ackKeyString;
                        awaitResponse = true;
                    }
                }

                var body = partitionEvent.Data.EventBody.ToArray();
                if (symmetricConfig is not null)
                    body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                var message = AzureEventHubCommon.Deserialize<AzureEventHubMessage>(body);
                if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                    throw new Exception("Invalid Message");

                var commandOrEvent = AzureEventHubCommon.Deserialize(message.MessageType, message.MessageData);
                if (commandOrEvent is null)
                    throw new Exception("Invalid Message");

                if (message.Claims is not null)
                {
                    var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                    Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                }

                inHandlerContext = true;
                if (isCommand)
                {
                    if (message.HasResult)
                        result = await handlerWithResultAwaitAsync!((ICommand)commandOrEvent, message.Source, false, cancellationToken);
                    else if (awaitResponse)
                        await handlerAwaitAsync!((ICommand)commandOrEvent, message.Source, false, cancellationToken);
                    else
                        await handlerAsync!((ICommand)commandOrEvent, message.Source, false, default);
                }
                else if (isEvent)
                {
                    await eventHandlerAsync!((IEvent)commandOrEvent, message.Source, false);
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
                if (isCommand && !awaitResponse)
                    commandCounter!.CompleteReceive(throttle);
                else
                    throttle.Release();
            }

            if (!awaitResponse)
                return;

            try
            {
                var acknowledgement = new Acknowledgement(result, error);
                var ackBody = AzureEventHubCommon.Serialize(acknowledgement);
                if (symmetricConfig is not null)
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
                if (isCommand)
                    commandCounter!.CompleteReceive(throttle);
                else
                    throttle.Release();
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
                canceller?.Cancel();
                isOpen = false;
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        void ICommandConsumer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (!this.maxConcurrent.HasValue || maxConcurrent < this.maxConcurrent.Value)
                this.maxConcurrent = maxConcurrent;
            commandTypes.Add(type);
        }

        void IEventConsumer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (!this.maxConcurrent.HasValue || maxConcurrent < this.maxConcurrent.Value)
                this.maxConcurrent = maxConcurrent;
            eventTypes.Add(type);
        }
    }
}
