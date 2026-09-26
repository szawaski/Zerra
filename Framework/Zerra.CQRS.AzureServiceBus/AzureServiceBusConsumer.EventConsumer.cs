// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer
    {
        public sealed class EventConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly string topic;
            private readonly string subscription;
            private readonly SymmetricConfig? symmetricConfig;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;

            public EventConsumer(int maxConcurrent, string topic, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = maxConcurrent;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(AzureServiceBusCommon.EntityNameMaxLength);

                this.subscription = $"EVT-{Guid.NewGuid():N}";
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, ServiceBusClient client) => Open(AzureServiceBusCommon.GetNamespace(host), client);

            internal void Open(AzureServiceBusCommonNamespace commonNamespace, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(commonNamespace, client, handlerAsync));
            }

            public Task ListeningThread(string host, ServiceBusClient client, HandleRemoteEventDispatch handlerAsync) => ListeningThread(AzureServiceBusCommon.GetNamespace(host), client, handlerAsync);

            private async Task ListeningThread(AzureServiceBusCommonNamespace commonNamespace, ServiceBusClient client, HandleRemoteEventDispatch handlerAsync)
            {
                //the throttle is not disposed: handlers still running after the listener stops release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await AzureServiceBusCommon.EnsureTopic(commonNamespace, topic, false);
                    await AzureServiceBusCommon.EnsureSubscription(commonNamespace, topic, subscription, true);

                    await using (var receiver = client.CreateReceiver(topic, subscription, receiverOptions))
                    {
                        for (; ; )
                        {
                            await throttle.WaitAsync(canceller.Token);

                            ServiceBusReceivedMessage? serviceBusMessage;
                            try
                            {
                                serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            }
                            catch
                            {
                                //the retry keeps this throttle, give back the permit taken for the message that wasn't received
                                _ = throttle.Release();
                                throw;
                            }
                            if (serviceBusMessage is null)
                            {
                                throttle.Release();
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
                        _ = Log.ErrorAsync(topic, ex);
                        await AzureServiceBusCommon.Forget(commonNamespace, topic);
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }

                //only reached once the consumer is stopping, a retry keeps the subscription so events sent meanwhile are still received
                try
                {
                    await AzureServiceBusCommon.DeleteSubscription(commonNamespace, topic, subscription);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
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
                        if (symmetricConfig is not null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body, false);

                        message = await AzureServiceBusCommon.DeserializeAsync<AzureServiceBusMessage>(body);
                    }
                    finally
                    {
                        body.Dispose();
                    }

                    if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                        throw new Exception("Invalid Message");

                    var @event = AzureServiceBusCommon.Deserialize(message.MessageData, message.MessageType) as IEvent;
                    if (@event is null)
                        throw new Exception("Invalid Message");

                    if (message.Claims is not null)
                    {
                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }

                    inHandlerContext = true;
                    await handlerAsync(@event, message.Source, false);
                    inHandlerContext = false;
                }
                catch (Exception ex)
                {
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(topic, ex);
                }
                finally
                {
                    throttle.Release();
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
