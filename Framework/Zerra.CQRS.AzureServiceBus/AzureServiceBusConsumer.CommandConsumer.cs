﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer
    {
        public sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly int? maxReceive;
            private readonly Action processExit;
            private readonly string topic;
            private readonly string subscription;
            private readonly SymmetricConfig symmetricConfig;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly CancellationTokenSource canceller = null;

            private readonly object countLocker = new();
            private int receivedCount;
            private int completedCount;

            public CommandConsumer(int maxConcurrent, int? maxReceive, Action processExit, string topic, SymmetricConfig symmetricConfig, string environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));
                if (maxReceive.HasValue && maxReceive.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(maxReceive));

                this.maxConcurrent = maxReceive.HasValue ? Math.Min(maxReceive.Value, maxConcurrent) : maxConcurrent;
                this.maxReceive = maxReceive;
                this.processExit = processExit;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{topic}".Truncate(AzureServiceBusCommon.TopicMaxLength);
                else
                    this.topic = topic.Truncate(AzureServiceBusCommon.TopicMaxLength);

                this.subscription = applicationName.Truncate(AzureServiceBusCommon.SubscriptionMaxLength);
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(host, client, handlerAsync, handlerAwaitAsync);
            }

            private async Task ListeningThread(string host, ServiceBusClient client, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                using var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await AzureServiceBusCommon.EnsureTopic(host, topic, false);
                    await AzureServiceBusCommon.EnsureSubscription(host, topic, subscription, false);

                    await using (var receiver = client.CreateReceiver(topic, subscription, receiverOptions))
                    {
                        for (; ; )
                        {
                            await throttle.WaitAsync();

                            if (maxReceive.HasValue)
                            {
                                lock (countLocker)
                                {
                                    if (receivedCount == maxReceive.Value)
                                        continue; //fill throttle, don't receive anymore, externally will be shutdown (shouldn't hit this line)
                                    receivedCount++;
                                }
                            }

                            var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            if (serviceBusMessage == null)
                                continue;

                            _ = HandleMessage(throttle, client, serviceBusMessage, handlerAsync, handlerAwaitAsync);

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
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                Exception error = null;
                var awaitResponse = false;
                string ackTopic = null;
                string ackKey = null;

                var inHandlerContext = false;
                try
                {
                    var body = serviceBusMessage.Body.ToStream();
                    AzureServiceBusCommandMessage message;
                    try
                    {
                        if (symmetricConfig != null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body, false);

                        message = await AzureServiceBusCommon.DeserializeAsync<AzureServiceBusCommandMessage>(body);
                    }
                    finally
                    {
                        body.Dispose();
                    }

                    awaitResponse = !String.IsNullOrWhiteSpace(serviceBusMessage.ReplyTo);
                    ackTopic = serviceBusMessage.ReplyTo;
                    ackKey = serviceBusMessage.ReplyToSessionId;

                    if (message.Claims != null)
                    {
                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }

                    inHandlerContext = true;
                    if (awaitResponse)
                        await handlerAwaitAsync(message.Message, message.Source, false);
                    else
                        await handlerAsync(message.Message, message.Source, false);
                    inHandlerContext = false;
                }
                catch (Exception ex)
                {
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(topic, ex);
                    error = ex;
                }
                finally
                {
                    if (!awaitResponse)
                    {
                        if (maxReceive.HasValue)
                        {
                            lock (countLocker)
                            {
                                completedCount++;
                                if (completedCount == maxReceive.Value)
                                    processExit?.Invoke();
                                else if (throttle.CurrentCount < maxReceive.Value - receivedCount)
                                    _ = throttle.Release(); //don't release more than needed to reach maxReceive
                            }
                        }
                        else
                        {
                            _ = throttle.Release();
                        }
                    }
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
                    var body = AzureServiceBusCommon.Serialize(ack);
                    if (symmetricConfig != null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    var replyServiceBusMessage = new ServiceBusMessage(body);
                    replyServiceBusMessage.SessionId = ackKey;
                    await using (var sender = client.CreateSender(ackTopic))
                    {
                        await sender.SendMessageAsync(replyServiceBusMessage);
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                }
                finally
                {
                    if (maxReceive.HasValue)
                    {
                        lock (countLocker)
                        {
                            completedCount++;

                            if (completedCount == maxReceive.Value)
                                processExit?.Invoke();
                            else if (throttle.CurrentCount < maxReceive.Value - receivedCount)
                                _ = throttle.Release(); //don't release more than needed to reach maxReceive
                        }
                    }
                    else
                    {
                        _ = throttle.Release();
                    }
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
