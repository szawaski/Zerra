﻿// Copyright © KaKush LLC
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
            private readonly SymmetricConfig symmetricConfig;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;

            public EventConsumer(int maxConcurrent, string topic, SymmetricConfig symmetricConfig, string environment, HandleRemoteEventDispatch handlerAsync)
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

            public void Open(string host, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(host, client, handlerAsync);
            }

            public async Task ListeningThread(string host, ServiceBusClient client, HandleRemoteEventDispatch handlerAsync)
            {
                
            retry:

                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                try
                {
                    await AzureServiceBusCommon.EnsureTopic(host, topic, false);
                    await AzureServiceBusCommon.EnsureSubscription(host, topic, subscription, true);

                    await using (var receiver = client.CreateReceiver(topic, subscription, receiverOptions))
                    {
                        for (; ; )
                        {
                            await throttle.WaitAsync(canceller.Token);

                            var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            if (serviceBusMessage == null)
                            {
                                throttle.Release();
                                continue;
                            }

                            _ = HandleMessage(throttle, client, serviceBusMessage, handlerAsync);

                            if (canceller.IsCancellationRequested)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(topic, ex);
                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }
                finally
                {
                    throttle.Dispose();
                    try
                    {
                        await AzureServiceBusCommon.DeleteSubscription(host, topic, subscription);
                    }
                    catch (Exception ex)
                    {
                        _ = Log.ErrorAsync(ex);
                    }
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage, HandleRemoteEventDispatch handlerAsync)
            {
                var inHandlerContext = false;
                try
                {
                    var body = serviceBusMessage.Body.ToStream();
                    AzureServiceBusEventMessage message;
                    try
                    {
                        if (symmetricConfig != null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body, false);

                        message = await AzureServiceBusCommon.DeserializeAsync<AzureServiceBusEventMessage>(body);
                    }
                    finally
                    {
                        body.Dispose();
                    }

                    if (message.Claims != null)
                    {
                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }

                    inHandlerContext = true;
                    await handlerAsync(message.Message, message.Source, false);
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
