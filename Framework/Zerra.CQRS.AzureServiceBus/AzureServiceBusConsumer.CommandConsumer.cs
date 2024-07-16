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
using Zerra.CQRS.Network;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer
    {
        public sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter commandCounter;
            private readonly string queue;
            private readonly SymmetricConfig? symmetricConfig;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly CancellationTokenSource canceller;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string queue, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.queue = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, queue);
                else
                    this.queue = queue.Truncate(AzureServiceBusCommon.EntityNameMaxLength);

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

            retry:

                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
                try
                {
                    await AzureServiceBusCommon.EnsureQueue(host, queue, false);

                    await using (var receiver = client.CreateReceiver(queue, receiverOptions))
                    {
                        for (; ; )
                        {
                            await throttle.WaitAsync(canceller.Token);

                            if (!commandCounter.BeginReceive())
                                continue; //don't receive anymore, externally will be shutdown, fill throttle

                            var serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            if (serviceBusMessage == null)
                            {
                                commandCounter.CancelReceive(throttle);
                                continue;
                            }

                            _ = HandleMessage(throttle, client, serviceBusMessage, handlerAsync, handlerAwaitAsync);

                            if (canceller.IsCancellationRequested)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(queue, ex);
                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }
                finally
                {
                    throttle.Dispose();
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                Exception? error = null;
                var awaitResponse = false;
                string? ackTopic = null;
                string? ackKey = null;

                var inHandlerContext = false;
                try
                {
                    var body = serviceBusMessage.Body.ToStream();
                    AzureServiceBusMessage? message;
                    try
                    {
                        if (symmetricConfig != null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body, false);

                        message = await AzureServiceBusCommon.DeserializeAsync<AzureServiceBusMessage>(body);
                    }
                    finally
                    {
                        body.Dispose();
                    }

                    if (message == null || message.MessageType == null || message.MessageData == null || message.Source == null)
                        throw new Exception("Invalid Message");

                    var command = AzureServiceBusCommon.Deserialize(message.MessageType, message.MessageData) as ICommand;
                    if (command == null)
                        throw new Exception("Invalid Message");

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
                        await handlerAwaitAsync(command, message.Source, false);
                    else
                        await handlerAsync(command, message.Source, false);
                    inHandlerContext = false;
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(queue, ex);
                }
                finally
                {
                    if (!awaitResponse)
                    {
                        commandCounter.CompleteReceive(throttle);
                    }
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ack = new Acknowledgement(error);

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
                    commandCounter.CompleteReceive(throttle);
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
