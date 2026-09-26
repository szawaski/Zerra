// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure.Messaging.ServiceBus;
using Zerra.Collections;
using System.Security.Claims;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
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
            private readonly CommandCounter commandCounter;
            private readonly string queue;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILogger? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);
            private Task? listening;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string queue, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.queue = StringExtensions.Join(AzureServiceBusCommon.EntityNameMaxLength, "_", environment, queue, out truncated);
                else
                    this.queue = queue.Truncate(AzureServiceBusCommon.EntityNameMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(AzureServiceBusConsumer)} truncated the command queue to {AzureServiceBusCommon.EntityNameMaxLength} characters: {this.queue}. Another queue truncating to the same name would be consumed as this one.");

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
                listening = Task.Run(() => ListeningThread(host, client));
            }

            private async Task ListeningThread(string host, ServiceBusClient client)
            {
                //the throttle is not disposed: handlers still running after the listener stops release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

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

                            ServiceBusReceivedMessage? serviceBusMessage;
                            try
                            {
                                serviceBusMessage = await receiver.ReceiveMessageAsync(null, canceller.Token);
                            }
                            catch
                            {
                                commandCounter.CancelReceive(throttle);
                                throw;
                            }
                            if (serviceBusMessage is null)
                            {
                                commandCounter.CancelReceive(throttle);
                                continue;
                            }

                            var handleTask = Task.Run(() => HandleMessage(throttle, client, serviceBusMessage));
                            //tracked until it completes so disposing waits for it
                            _ = handling.Add(handleTask);
                            _ = handleTask.ContinueWith(removeHandling, handling, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

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
                        log?.Error(queue, ex);
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

                    var command = serializer.Deserialize(message.MessageData, TypeFinder.GetTypeFromName(message.MessageType)) as ICommand;
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
                        commandCounter.CompleteReceive(throttle);
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = serviceBusMessage.ReplyTo;
                    var ackKey = serviceBusMessage.ReplyToSessionId;

                    var acknowledgement = new Acknowledgement(serializer, result, error);

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
