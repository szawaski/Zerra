// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using Zerra.Collections;
using System.Security.Claims;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.CQRS.AzureServiceBus
{
    public sealed partial class AzureServiceBusConsumer
    {
        private sealed class CommandConsumer : IDisposable, IAsyncDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter? commandCounter;
            private readonly string queue;
            private readonly ConcurrentDictionary<string, Type> commandTypes;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ICompressor? compressor;
            private readonly ILogger? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly bool resilient;
            private readonly CancellationTokenSource canceller;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);
            private Task? listening;

            //One sender per producer's acknowledgement queue, reused for every reply to it instead of opening a link to the broker per reply.
            //The broker deletes an acknowledgement queue once it's idle for DeleteWhenIdleTimeout, so a sender idle that long is dropped by the next sweep,
            //which keeps the senders to the producers still sending instead of every producer ever seen.
            private readonly ConcurrentDictionary<string, ReplySender> replySenders = new();
            private static readonly int replySenderIdleMilliseconds = (int)AzureServiceBusCommon.DeleteWhenIdleTimeout.TotalMilliseconds;
            private const int maxAckAttempts = 12;
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

            public CommandConsumer(int maxConcurrent, CommandCounter? commandCounter, string queue, ConcurrentDictionary<string, Type> commandTypes, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync, bool resilient)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter is not null ? Math.Min(commandCounter.ReceiveCountBeforeExit, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.queue = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, queue, out truncated);
                else
                    this.queue = queue.Truncate(AzureServiceBusCommon.EntityNameMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(AzureServiceBusConsumer)} truncated the command queue to {AzureServiceBusCommon.EntityNameMaxLength} characters: {this.queue}. Another queue truncating to the same name would be consumed as this one.");

                this.commandTypes = commandTypes;
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.compressor = compressor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.resilient = resilient;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(AzureServiceBusCommonNamespace commonNamespace, ServiceBusClient client)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                listening = Task.Run(() => ListeningThread(commonNamespace, client));
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

                    await using (var receiver = client.CreateReceiver(queue, resilient ? resilientReceiverOptions : receiverOptions))
                    {
                        try
                        {
                            for (; ; )
                            {
                                await throttle.WaitAsync(canceller.Token);

                                if (commandCounter is not null && !commandCounter.BeginReceive())
                                    break; //don't receive anymore, externally will be shutdown

                                ServiceBusReceivedMessage? serviceBusMessage;
                                try
                                {
                                    serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                                }
                                catch
                                {
                                    if (commandCounter is not null)
                                        commandCounter.CancelReceive(throttle);
                                    else
                                        _ = throttle.Release();
                                    throw;
                                }
                                if (serviceBusMessage is null)
                                {
                                    if (commandCounter is not null)
                                        commandCounter.CancelReceive(throttle);
                                    else
                                        _ = throttle.Release();
                                    continue;
                                }

                                var handleTask = Task.Run(() => HandleMessage(throttle, client, serviceBusMessage, resilient ? receiver : null));
                                //tracked until it completes so disposing waits for it
                                _ = handling.Add(handleTask);
                                _ = handleTask.ContinueWith(removeHandling, handling, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                                if (canceller.IsCancellationRequested || (commandCounter is not null && commandCounter.ReceiveLimitReached))
                                    break;
                            }
                        }
                        finally
                        {
                            //resilient commands are completed through this receiver, closing it first would have them delivered again
                            if (resilient)
                            {
                                for (; ; )
                                {
                                    var pending = handling.Where(x => !x.IsCompleted).ToArray();
                                    if (pending.Length == 0)
                                        break;
                                    try
                                    {
                                        await Task.WhenAll(pending);
                                    }
                                    catch (Exception ex)
                                    {
                                        log?.Error(queue, ex);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //closing cancels the receive, that isn't an error
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(queue, ex);
                        await AzureServiceBusCommon.Forget(commonNamespace, queue);
                        await Task.Delay(AzureServiceBusCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            //a resilient command's lock is renewed while it's handled, otherwise it would be delivered again to another consumer when it expires
            private async Task RenewLock(ServiceBusReceiver receiver, ServiceBusReceivedMessage serviceBusMessage, CancellationToken cancellationToken)
            {
                try
                {
                    for (; ; )
                    {
                        //half of what's left so a clock difference with the broker doesn't let it expire
                        var delay = TimeSpan.FromTicks((serviceBusMessage.LockedUntil - DateTimeOffset.UtcNow).Ticks / 2);
                        if (delay < TimeSpan.FromSeconds(1))
                            delay = TimeSpan.FromSeconds(1);
                        await Task.Delay(delay, cancellationToken);
                        try
                        {
                            await receiver.RenewMessageLockAsync(serviceBusMessage, cancellationToken);
                        }
                        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessageLockLost)
                        {
                            log?.Error(queue, ex);
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                        {
                            log?.Error(queue, ex);
                        }
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ServiceBusClient client, ServiceBusReceivedMessage serviceBusMessage, ServiceBusReceiver? receiver)
            {
                object? result = null;
                Exception? error = null;
                var awaitResponse = !String.IsNullOrWhiteSpace(serviceBusMessage.ReplyTo);

                CancellationTokenSource? renewing = null;
                Task? renewal = null;
                if (receiver is not null)
                {
                    renewing = new CancellationTokenSource();
                    renewal = RenewLock(receiver, serviceBusMessage, renewing.Token);
                }

                var inHandlerContext = false;
                try
                {
                    var body = serviceBusMessage.Body.ToStream();
                    AzureServiceBusMessage? message;
                    try
                    {
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body, false);
                        if (compressor is not null)
                            body = compressor.Decompress(body, false);

                        message = await serializer.DeserializeAsync<AzureServiceBusMessage>(body, CancellationToken.None);
                    }
                    finally
                    {
                        body.Dispose();
                    }

                    if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                        throw new Exception("Invalid Message");

                    if (!commandTypes.TryGetValue(message.MessageType, out var commandType))
                        throw new Exception($"Unhandled Message Type {message.MessageType}");
                    var command = serializer.Deserialize(message.MessageData, commandType) as ICommand;
                    if (command is null)
                        throw new Exception("Invalid Message");

                    if (message.Claims is not null)
                    {
                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                    }

                    inHandlerContext = true;
                    if (message.HasResult)
                        result = await handlerWithResultAwaitAsync(command, message.Source, CancellationToken.None); //closing lets it finish instead of cancelling it
                    else if (awaitResponse)
                        await handlerAwaitAsync(command, message.Source, CancellationToken.None);
                    else
                        await handlerAsync(command, message.Source, default);
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
                    {
                        if (receiver is not null)
                        {
                            renewing!.Cancel();
                            await renewal!;
                            renewing.Dispose();
                            try
                            {
                                await receiver.CompleteMessageAsync(serviceBusMessage);
                            }
                            catch (Exception ex)
                            {
                                log?.Error(queue, ex);
                            }
                        }
                        if (commandCounter is not null)
                            commandCounter.CompleteReceive(throttle);
                        else
                            _ = throttle.Release();
                    }
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = serviceBusMessage.ReplyTo;
                    var ackKey = serviceBusMessage.ReplyToSessionId;

                    var acknowledgement = new Acknowledgement(serializer, result, error);

                    var body = serializer.SerializeBytes(acknowledgement);
                    if (compressor is not null)
                        body = compressor.Compress(body);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

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
                        for (var attempt = 1; ; attempt++)
                        {
                            try
                            {
                                await replySender.Sender.SendMessageAsync(replyServiceBusMessage);
                                break;
                            }
                            catch (ServiceBusException ex) when (attempt < maxAckAttempts && ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
                            {
                                if (attempt == 1)
                                    log?.Warn($"{nameof(AzureServiceBusConsumer)} failed to send an acknowledgement to {ackTopic}, trying again until its producer creates it again: {ex.Message}");
                                await Task.Delay(AzureServiceBusCommon.RetryDelay);
                            }
                        }
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
                    log?.Error(ex);
                }
                finally
                {
                    if (receiver is not null)
                    {
                        renewing!.Cancel();
                        await renewal!;
                        renewing.Dispose();
                        try
                        {
                            await receiver.CompleteMessageAsync(serviceBusMessage);
                        }
                        catch (Exception ex)
                        {
                            log?.Error(queue, ex);
                        }
                    }
                    if (commandCounter is not null)
                        commandCounter.CompleteReceive(throttle);
                    else
                        _ = throttle.Release();
                }
            }

            public void Close()
            {
                canceller.Cancel();
            }

            public void Dispose()
            {
                canceller.Cancel();

                //waits for the listener to stop so nothing new starts, then for the messages it already received, the ones that completed were already removed
                if (listening is not null)
                {
                    try
                    {
                        listening.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        log?.Error(queue, ex.InnerException ?? ex);
                    }
                }
                for (; ; )
                {
                    var pending = handling.Where(x => !x.IsCompleted).ToArray();
                    if (pending.Length == 0)
                        break;
                    try
                    {
                        Task.WaitAll(pending);
                    }
                    catch (AggregateException ex)
                    {
                        log?.Error(queue, ex.InnerException ?? ex);
                    }
                }

                canceller.Dispose();
            }

            public async ValueTask DisposeAsync()
            {
                canceller.Cancel();

                //waits for the listener to stop so nothing new starts, then for the messages it already received, the ones that completed were already removed
                if (listening is not null)
                {
                    try
                    {
                        await listening;
                    }
                    catch (Exception ex)
                    {
                        log?.Error(queue, ex);
                    }
                }
                for (; ; )
                {
                    var pending = handling.Where(x => !x.IsCompleted).ToArray();
                    if (pending.Length == 0)
                        break;
                    try
                    {
                        await Task.WhenAll(pending);
                    }
                    catch (Exception ex)
                    {
                        log?.Error(queue, ex);
                    }
                }

                canceller.Dispose();
            }
        }
    }
}
