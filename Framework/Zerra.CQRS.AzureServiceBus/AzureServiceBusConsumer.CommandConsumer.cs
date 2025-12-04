// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System.Security.Claims;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer
    {
        private sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter commandCounter;
            private readonly string queue;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILog? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string queue, ISerializer serializer, IEncryptor? encryptor, ILog? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.queue = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, queue);
                else
                    this.queue = queue.Truncate(AzureServiceBusCommon.EntityNameMaxLength);

                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, client));
            }

            private async Task ListeningThread(string host, ServiceBusClient client)
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
                            if (serviceBusMessage is null)
                            {
                                commandCounter.CancelReceive(throttle);
                                continue;
                            }

                            _ = Task.Run(() => HandleMessage(throttle, client, serviceBusMessage));

                            if (canceller.IsCancellationRequested)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(queue, ex);
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

            private async Task HandleMessage(SemaphoreSlim throttle, ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage)
            {
                object? result = null;
                Exception? error = null;
                var awaitResponse = !String.IsNullOrWhiteSpace(serviceBusMessage.ReplyTo);

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

                    var command = serializer.Deserialize(message.MessageData, message.MessageType) as ICommand;
                    if (command is null)
                        throw new Exception("Invalid Message");

                    if (message.Claims is not null)
                    {
                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }

                    inHandlerContext = true;
                    if (message.HasResult)
                        result = await handlerWithResultAwaitAsync(command, message.Source, false, canceller.Token);
                    else if (awaitResponse)
                        await handlerAwaitAsync(command, message.Source, false, canceller.Token);
                    else
                        await handlerAsync(command, message.Source, false, default);
                    inHandlerContext = false;
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (!inHandlerContext)
                        log?.Error(queue, ex);
                }
                finally
                {
                    if (!awaitResponse)
                        commandCounter.CompleteReceive(throttle);
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = serviceBusMessage.ReplyTo;
                    var ackKey = serviceBusMessage.ReplyToSessionId;

                    var acknowledgement = new Acknowledgement(result, error);

                    var body = serializer.SerializeBytes(acknowledgement);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    var replyServiceBusMessage = new ServiceBusMessage(body);
                    replyServiceBusMessage.SessionId = ackKey;
                    await using (var sender = client.CreateSender(ackTopic))
                    {
                        await sender.SendMessageAsync(replyServiceBusMessage);
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
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
