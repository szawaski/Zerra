// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System.Security.Claims;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer
    {
        private sealed class EventConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly string topic;
            private readonly string subscription;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILogger? log;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;
            //PerReplica gets a subscription of its own so this replica receives every event, it's deleted when the consumer stops
            //PerService gets a subscription named for the service so its replicas compete for the events, it's shared so it's never deleted
            private readonly bool deleteSubscriptionOnStop;

            public EventConsumer(int maxConcurrent, string topic, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, string serviceName, EventConsumerMode eventConsumerMode, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = maxConcurrent;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(AzureServiceBusCommon.EntityNameMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(AzureServiceBusConsumer)} truncated the event topic to {AzureServiceBusCommon.EntityNameMaxLength} characters: {this.topic}. Another topic truncating to the same name would be consumed as this one.");

                if (eventConsumerMode == EventConsumerMode.PerService)
                {
                    this.subscription = $"EVT-{serviceName}".Truncate(AzureServiceBusCommon.EntityNameMaxLength, out truncated);
                    if (truncated)
                        log?.Warn($"{nameof(AzureServiceBusConsumer)} truncated the {EventConsumerMode.PerService} subscription to {AzureServiceBusCommon.EntityNameMaxLength} characters: {this.subscription}. Another service truncating to the same subscription would compete with this one for the events.");
                    this.deleteSubscriptionOnStop = false;
                }
                else
                {
                    this.subscription = $"EVT-{Guid.NewGuid():N}";
                    this.deleteSubscriptionOnStop = true;
                }
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, client, handlerAsync));
            }

            public async Task ListeningThread(string host, ServiceBusClient client, HandleRemoteEventDispatch handlerAsync)
            {

            retry:

                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                try
                {
                    await AzureServiceBusCommon.EnsureTopic(host, topic, false);
                    await AzureServiceBusCommon.EnsureSubscription(host, topic, subscription, deleteSubscriptionOnStop);

                    await using (var receiver = client.CreateReceiver(topic, subscription, receiverOptions))
                    {
                        for (; ; )
                        {
                            await throttle.WaitAsync(canceller.Token);

                            var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            if (serviceBusMessage is null)
                            {
                                _ = throttle.Release();
                                continue;
                            }

                            _ = Task.Run(() => HandleMessage(throttle, client, serviceBusMessage, handlerAsync));

                            if (canceller.IsCancellationRequested)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //closing cancels the receive, that isn't an error
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(topic, ex);
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }
                finally
                {
                    throttle.Dispose();
                }

                //only reached once the consumer is stopping, a retry keeps the subscription so events sent meanwhile are still received
                //a PerService subscription belongs to the other replicas too so it stays
                if (deleteSubscriptionOnStop)
                {
                    try
                    {
                        await AzureServiceBusCommon.DeleteSubscription(host, topic, subscription);
                    }
                    catch (Exception ex)
                    {
                        log?.Error(ex);
                    }
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage, HandleRemoteEventDispatch handlerAsync)
            {
                var inHandlerContext = false;
                try
                {
                    var body = serviceBusMessage.Body.ToStream();
                    AzureServiceBusMessage? message;
                    try
                    {
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body, false);

                        message = await serializer.DeserializeAsync<AzureServiceBusMessage>(body, CancellationToken.None);
                    }
                    finally
                    {
                        body.Dispose();
                    }

                    if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                        throw new Exception("Invalid Message");

                    var @event = serializer.Deserialize(message.MessageData, TypeFinder.GetTypeFromName(message.MessageType)) as IEvent;
                    if (@event is null)
                        throw new Exception("Invalid Message");

                    if (message.Claims is not null)
                    {
                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }

                    inHandlerContext = true;
                    await handlerAsync(@event, message.Source);
                    inHandlerContext = false;
                }
                catch (Exception ex)
                {
                    if (!inHandlerContext)
                        log?.Error(topic, ex);
                }
                finally
                {
                    _ = throttle.Release();
                }
            }

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();
            }
        }
    }
}
