// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;

            //One sender per producer's acknowledgement queue, reused for every reply to it instead of opening a link to the broker per reply.
            //The broker deletes an acknowledgement queue once it's idle for DeleteWhenIdleTimeout, so a sender idle that long is dropped by the next sweep,
            //which keeps the senders to the producers still sending instead of every producer ever seen.
            private readonly ConcurrentDictionary<string, ReplySender> replySenders = new();
            private static readonly int replySenderIdleMilliseconds = (int)AzureServiceBusCommon.DeleteWhenIdleTimeout.TotalMilliseconds;
            private int lastReplySenderSweep = Environment.TickCount;

            private sealed class ReplySender
            {
                public readonly ServiceBusSender Sender;
                //Environment.TickCount, its wrap around doesn't matter to differences this short
                public int LastUsed;
                public ReplySender(ServiceBusSender sender, int lastUsed)
                {
                    this.Sender = sender;
                    this.LastUsed = lastUsed;
                }
            }

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string queue, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
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
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, ServiceBusClient client) => Open(AzureServiceBusCommon.GetNamespace(host), client);

            internal void Open(AzureServiceBusCommonNamespace commonNamespace, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(commonNamespace, client));
            }

            private async Task ListeningThread(AzureServiceBusCommonNamespace commonNamespace, ServiceBusClient client)
            {
                //the throttle is not disposed: handlers still running after the listener stops release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await AzureServiceBusCommon.EnsureQueue(commonNamespace, queue, false);

                    await using (var receiver = client.CreateReceiver(queue, receiverOptions))
                    {
                        for (; ; )
                        {
                            await throttle.WaitAsync(canceller.Token);

                            if (!commandCounter.BeginReceive())
                                break; //don't receive anymore, externally will be shutdown

                            ServiceBusReceivedMessage? serviceBusMessage;
                            try
                            {
                                serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            }
                            catch
                            {
                                //the retry keeps this throttle, give back the permit taken for the message that wasn't received
                                commandCounter.CancelReceive(throttle);
                                throw;
                            }
                            if (serviceBusMessage is null)
                            {
                                commandCounter.CancelReceive(throttle);
                                continue;
                            }

                            _ = Task.Run(() => HandleMessage(throttle, client, serviceBusMessage));

                            if (canceller.IsCancellationRequested || commandCounter.ReceiveLimitReached)
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //closing cancels the receive, that isn't an error
                    if (!canceller.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(queue, ex);
                        await AzureServiceBusCommon.Forget(commonNamespace, queue);
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
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

                    var command = AzureServiceBusCommon.Deserialize(message.MessageData, message.MessageType) as ICommand;
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
                        _ = Log.ErrorAsync(queue, ex);
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

                    var body = AzureServiceBusCommon.Serialize(acknowledgement);
                    if (symmetricConfig is not null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    var replyServiceBusMessage = new ServiceBusMessage(body);
                    replyServiceBusMessage.SessionId = ackKey;

                    var now = Environment.TickCount;
                    if (!replySenders.TryGetValue(ackTopic, out var replySender))
                    {
                        var created = new ReplySender(client.CreateSender(ackTopic), now);
                        replySender = replySenders.GetOrAdd(ackTopic, created);
                        if (replySender != created)
                            await created.Sender.DisposeAsync();
                    }
                    replySender.LastUsed = now;

                    try
                    {
                        await replySender.Sender.SendMessageAsync(replyServiceBusMessage);
                    }
                    catch
                    {
                        //the link failed or a sweep disposed this sender as it was taken, it's replaced and the reply sent once more on a new one
                        if (((ICollection<KeyValuePair<string, ReplySender>>)replySenders).Remove(new KeyValuePair<string, ReplySender>(ackTopic, replySender)))
                            await replySender.Sender.DisposeAsync();
                        var created = new ReplySender(client.CreateSender(ackTopic), now);
                        replySender = replySenders.GetOrAdd(ackTopic, created);
                        if (replySender != created)
                            await created.Sender.DisposeAsync();
                        await replySender.Sender.SendMessageAsync(replyServiceBusMessage);
                    }

                    //at most once per idle timeout, the senders to acknowledgement queues the broker has deleted by now are dropped
                    if (unchecked(now - lastReplySenderSweep) > replySenderIdleMilliseconds)
                    {
                        lastReplySenderSweep = now;
                        foreach (var pair in replySenders)
                        {
                            if (unchecked(now - pair.Value.LastUsed) > replySenderIdleMilliseconds && ((ICollection<KeyValuePair<string, ReplySender>>)replySenders).Remove(pair))
                                await pair.Value.Sender.DisposeAsync();
                        }
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
