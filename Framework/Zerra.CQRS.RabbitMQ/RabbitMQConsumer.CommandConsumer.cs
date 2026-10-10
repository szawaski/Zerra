// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Compression;
using Zerra.Encryption;
using Zerra.Logging;
using System.Collections.Concurrent;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed partial class RabbitMQConsumer
    {
        private sealed class CommandConsumer : IDisposable, IAsyncDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter? commandCounter;
            private readonly string topic;
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
#if NETSTANDARD2_0
            private readonly object isOpenLock = new();
#else
            private readonly Lock isOpenLock = new();
#endif

            private IChannel? channel = null;
            //a channel isn't safe to publish on from more than one thread, a publish is several frames and replies from concurrent handlers would
            //interleave them, acks are taken on the dispatcher while the handlers reply from their own tasks so they share the lock
            private readonly SemaphoreSlim channelLock = new(1, 1);
            private string? consumerTag = null;
            private string? cancelledConsumerTag = null;
            private readonly SemaphoreSlim cancelLock = new(1, 1);
            private volatile bool receiveLimitReached = false;
            private readonly SemaphoreSlim throttle;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);

            public CommandConsumer(int maxConcurrent, CommandCounter? commandCounter, string topic, ConcurrentDictionary<string, Type> commandTypes, ISerializer serializer, IEncryptor? encryptor, ICompressor? compressor, ILogger? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync, bool resilient)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter is not null ? Math.Min(commandCounter.ReceiveCountBeforeExit, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(RabbitMQConsumer)} truncated the command exchange and queue to {RabbitMQCommon.TopicMaxLength} characters: {this.topic}. Another exchange truncating to the same name would be consumed as this one.");
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
                this.throttle = new SemaphoreSlim(this.maxConcurrent, this.maxConcurrent);
            }

            public void Open(Func<Task<IConnection>> getConnection)
            {
                lock (isOpenLock)
                {
                    if (IsOpen)
                        return;
                    IsOpen = true;
                }
                _ = Task.Run(() => ListeningThread(getConnection));
            }

            private async Task ListeningThread(Func<Task<IConnection>> getConnection)
            {
            retry:

                try
                {
                    var connection = await getConnection();
                    if (canceller.IsCancellationRequested)
                        return;

                    if (this.channel is null)
                    {
                        this.channel = await connection.CreateChannelAsync();
                        await this.channel.BasicQosAsync(0, (ushort)maxConcurrent, false);
                    }
                    await this.channel.ExchangeDeclareAsync(this.topic, ExchangeType.Direct);

                    //not auto-deleted so commands sent while no replica is connected, such as during a reconnect, wait in the queue
                    var queue = await this.channel.QueueDeclareAsync(this.topic, true, false, false);
                    await this.channel.QueueBindAsync(queue.QueueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.ReceivedAsync += async (sender, e) =>
                    {
                        if (receiveLimitReached)
                        {
                            //waits for a cancel in progress so the command put back can't be sent here again
                            await StopReceivingAsync(consumer, e.ConsumerTag);
                            await RequeueAsync(consumer, e.DeliveryTag);
                            return;
                        }

                        try
                        {
                            await throttle.WaitAsync(canceller.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }

                        if (commandCounter is not null)
                        {
                            if (!commandCounter.BeginReceive())
                            {
                                _ = throttle.Release();
                                await StopReceivingAsync(consumer, e.ConsumerTag);
                                await RequeueAsync(consumer, e.DeliveryTag);
                                return; //don't receive anymore, externally will be shutdown
                            }

                            //cancelled before the ack, otherwise the broker keeps sending commands here that would have to be put back
                            if (commandCounter.ReceiveLimitReached)
                                await StopReceivingAsync(consumer, e.ConsumerTag);
                        }

                        if (!resilient)
                        {
                            await channelLock.WaitAsync();
                            try
                            {
                                await consumer.Channel.BasicAckAsync(e.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                log?.Error(topic, ex);
                                if (commandCounter is not null)
                                {
                                    commandCounter.CancelReceive(throttle);
                                    //the consumer was cancelled for this command, which can be received again
                                    if (receiveLimitReached)
                                    {
                                        receiveLimitReached = false;
                                        _ = Task.Run(() => ListeningThread(getConnection));
                                    }
                                }
                                else
                                {
                                    _ = throttle.Release();
                                }
                                return;
                            }
                            finally
                            {
                                _ = channelLock.Release();
                            }
                        }

                        var body = e.Body.ToArray();
                        var replyTo = e.BasicProperties.ReplyTo;
                        var correlationId = e.BasicProperties.CorrelationId;
                        //resilient commands are acknowledged once handled, on the channel they came from, if it closes first the broker delivers them again
                        var ackChannel = resilient ? consumer.Channel : null;
                        var deliveryTag = e.DeliveryTag;
                        var handleTask = Task.Run(() => HandleMessage(body, replyTo, correlationId, ackChannel, deliveryTag));
                        _ = handling.Add(handleTask);
                        _ = handleTask.ContinueWith(removeHandling, handling, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    };

                    //raised for a cancel from the broker, such as the queue being deleted, as well as for the ones made here
                    consumer.UnregisteredAsync += (sender, e) =>
                    {
                        if (!canceller.IsCancellationRequested && !e.ConsumerTags.Contains(Volatile.Read(ref cancelledConsumerTag)) && consumer.Channel.IsOpen)
                            _ = Task.Run(() => ListeningThread(getConnection));
                        return Task.CompletedTask;
                    };

                    this.consumerTag = await this.channel.BasicConsumeAsync(queue.QueueName, false, consumer);
                }
                catch (Exception ex)
                {
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(topic, ex);

                        if (channel is not null)
                        {
                            //disposing closes the channel, ignoring one the broker already closed
                            await channel.DisposeAsync();
                            channel = null;
                        }
                        await Task.Delay(RabbitMQCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            private Task StopReceivingAsync(AsyncEventingBasicConsumer consumer, string consumerTag)
            {
                receiveLimitReached = true;
                return CancelConsumerAsync(consumer.Channel, consumerTag);
            }

            private async Task RequeueAsync(AsyncEventingBasicConsumer consumer, ulong deliveryTag)
            {
                await channelLock.WaitAsync();
                try
                {
                    await consumer.Channel.BasicNackAsync(deliveryTag, false, true);
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);
                }
                finally
                {
                    _ = channelLock.Release();
                }
            }

            //the limit and Close can both cancel the same consumer, it's only cancelled once and a second caller waits for the broker to confirm it
            private async Task CancelConsumerAsync(IChannel channel, string consumerTag)
            {
                await cancelLock.WaitAsync();
                try
                {
                    if (cancelledConsumerTag == consumerTag)
                        return;
                    try
                    {
                        await channel.BasicCancelAsync(consumerTag);
                    }
                    catch (Exception ex)
                    {
                        //Dispose closes the channel without waiting for a cancel from Close
                        if (channel.IsOpen)
                            log?.Error(topic, ex);
                    }
                    Volatile.Write(ref cancelledConsumerTag, consumerTag);
                }
                finally
                {
                    _ = cancelLock.Release();
                }
            }

            private async Task HandleMessage(byte[] body, string? replyTo, string? correlationId, IChannel? ackChannel, ulong deliveryTag)
            {
                object? result = null;
                Exception? error = null;
                var awaitResponse = !String.IsNullOrWhiteSpace(replyTo);

                var inHandlerContext = false;
                try
                {
                    if (encryptor is not null)
                        body = encryptor.Decrypt(body);
                    if (compressor is not null)
                        body = compressor.Decompress(body);

                    var message = serializer.Deserialize<RabbitMQMessage>(body);
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
                    if (!inHandlerContext)
                        log?.Error(topic, ex);

                    error = ex;
                }
                finally
                {
                    if (!awaitResponse)
                    {
                        if (ackChannel is not null)
                        {
                            await channelLock.WaitAsync();
                            try
                            {
                                await ackChannel.BasicAckAsync(deliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                log?.Error(topic, ex);
                            }
                            finally
                            {
                                _ = channelLock.Release();
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
                    var acknowledgement = new Acknowledgement(serializer, result, error);

                    var acknowledgmentBody = serializer.SerializeBytes(acknowledgement);
                    if (compressor is not null)
                        acknowledgmentBody = compressor.Compress(acknowledgmentBody);
                    if (encryptor is not null)
                        acknowledgmentBody = encryptor.Encrypt(acknowledgmentBody);

                    var replyProperties = new BasicProperties() { CorrelationId = correlationId };
                    await channelLock.WaitAsync();
                    try
                    {
                        await this.channel!.BasicPublishAsync(String.Empty, replyTo!, false, replyProperties, acknowledgmentBody);
                    }
                    finally
                    {
                        _ = channelLock.Release();
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);
                }
                finally
                {
                    if (ackChannel is not null)
                    {
                        await channelLock.WaitAsync();
                        try
                        {
                            await ackChannel.BasicAckAsync(deliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            log?.Error(topic, ex);
                        }
                        finally
                        {
                            _ = channelLock.Release();
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
                if (canceller.IsCancellationRequested)
                    return;
                canceller.Cancel();
                var channel = this.channel;
                var consumerTag = this.consumerTag;
                if (channel is not null && consumerTag is not null)
                    _ = CancelConsumerAsync(channel, consumerTag);
            }

            public void Dispose()
            {
                Close();

                //waits for the messages already received, the ones that completed were already removed
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
                        log?.Error(topic, ex.InnerException ?? ex);
                    }
                }

                canceller.Dispose();

                //the throttle is not disposed: handlers still running after Dispose release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished

                if (channel is not null)
                {
                    channel.Dispose();
                    channel = null;
                }

                GC.SuppressFinalize(this);
            }

            public async ValueTask DisposeAsync()
            {
                Close();

                //waits for the messages already received, the ones that completed were already removed
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
                        log?.Error(topic, ex);
                    }
                }

                canceller.Dispose();

                //the throttle is not disposed, see Dispose

                if (channel is not null)
                {
                    await channel.DisposeAsync();
                    channel = null;
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
