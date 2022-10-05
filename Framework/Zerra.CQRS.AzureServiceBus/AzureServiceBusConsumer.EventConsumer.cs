// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public partial class AzureServiceBusConsumer
    {
        public class EventConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricConfig symmetricConfig;
            private CancellationTokenSource canceller;

            public EventConsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(AzureServiceBusCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(AzureServiceBusCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
            }

            public void Open(string host, ServiceBusClient client, Func<IEvent, Task> handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, client, handlerAsync));
            }

            public async Task ListeningThread(string host, ServiceBusClient client, Func<IEvent, Task> handlerAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                try
                {
                    var subscription = $"{topic.Truncate(AzureServiceBusCommon.SubscriptionMaxLength / 2 - 1)}-{applicationName.Truncate(AzureServiceBusCommon.SubscriptionMaxLength / 2 - 1)}";
                    await AzureServiceBusCommon.EnsureTopic(host, topic, false);
                    await AzureServiceBusCommon.EnsureSubscription(host, topic, subscription, false);

                    _ = Log.TraceAsync($"Received: {topic}");

                    await using (var receiver = client.CreateReceiver(topic, topic))
                    {
                        for (; ; )
                        {
                            try
                            {
                                var serviceBusMessage = await receiver.ReceiveMessageAsync();
                                if (serviceBusMessage == null)
                                    continue;
                                await receiver.CompleteMessageAsync(serviceBusMessage);

                                var stopwatch = Stopwatch.StartNew();

                                var body = serviceBusMessage.Body.ToStream();
                                if (symmetricConfig != null)
                                    body = SymmetricEncryptor.Decrypt(symmetricConfig, body, false);

                                var message = await AzureServiceBusCommon.DeserializeAsync<AzureServiceBusEventMessage>(body);

                                if (message.Claims != null)
                                {
                                    var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                    Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                                }

                                await handlerAsync(message.Message);
                            }
                            catch (TaskCanceledException ex)
                            {
                                _ = Log.ErrorAsync($"Error: {topic}", ex);
                                break;
                            }
                            catch (Exception ex)
                            {
                                _ = Log.ErrorAsync($"Error: {topic}", ex);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync($"Error: {topic}", ex);
                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }
                canceller.Dispose();
                IsOpen = false;
            }

            public void Dispose()
            {
                if (canceller != null)
                    canceller.Cancel();
            }
        }
    }
}
