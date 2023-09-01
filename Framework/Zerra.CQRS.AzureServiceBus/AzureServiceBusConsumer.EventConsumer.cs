﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.IO;
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
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly string subscription;
            private readonly SymmetricConfig symmetricConfig;
            private CancellationTokenSource canceller;

            public EventConsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(AzureServiceBusCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(AzureServiceBusCommon.TopicMaxLength);

                this.subscription = $"EVENT-{Guid.NewGuid().ToString("N")}";
                this.symmetricConfig = symmetricConfig;
            }

            public void Open(string host, ServiceBusClient client, EventHandlerDelegate handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, client, handlerAsync));
            }

            public async Task ListeningThread(string host, ServiceBusClient client, EventHandlerDelegate handlerAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                try
                {
                    await AzureServiceBusCommon.EnsureTopic(host, topic, false);
                    await AzureServiceBusCommon.EnsureSubscription(host, topic, subscription, true);

                    await using (var receiver = client.CreateReceiver(topic, subscription))
                    {
                        for (; ; )
                        {

                            var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            if (serviceBusMessage == null)
                                continue;
                            await receiver.CompleteMessageAsync(serviceBusMessage);

                            _ = Log.TraceAsync($"Received: {topic}");

                            _ = HandleMessage(client, serviceBusMessage, handlerAsync);

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
                    try
                    {
                        await AzureServiceBusCommon.DeleteSubscription(host, topic, subscription);
                    }
                    catch (Exception ex)
                    {
                        _ = Log.ErrorAsync(ex);
                    }
                    canceller.Dispose();
                    IsOpen = false;
                }
            }

            private async Task HandleMessage(ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage, EventHandlerDelegate handlerAsync)
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
                    await handlerAsync(message.Message, message.Source);
                    inHandlerContext = false;
                }
                catch (Exception ex)
                {
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(topic, ex);
                }
            }

            public void Dispose()
            {
                if (canceller != null)
                    canceller.Cancel();
            }
        }
    }
}
