// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Reflection;
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
            private readonly CommandCounter commandCounter;
            private readonly string topic;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILogger? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
#if NETSTANDARD2_0
            private readonly object isOpenLock = new();
#else
            private readonly Lock isOpenLock = new();
#endif

            private IModel? channel = null;
            private string? consumerTag = null;
            private readonly SemaphoreSlim throttle;
            private readonly ConcurrentHashSet<Task> handling = new();
            private static readonly Action<Task, object?> removeHandling = static (task, state) => _ = ((ConcurrentHashSet<Task>)state!).Remove(task);

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, ISerializer serializer, IEncryptor? encryptor, ILogger? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                bool truncated;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic, out truncated);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength, out truncated);
                if (truncated)
                    log?.Warn($"{nameof(RabbitMQConsumer)} truncated the command exchange and queue to {RabbitMQCommon.TopicMaxLength} characters: {this.topic}. Another exchange truncating to the same name would be consumed as this one.");
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
                this.throttle = new SemaphoreSlim(this.maxConcurrent, this.maxConcurrent);
            }

            public void Open(IConnection connection)
            {
                lock (isOpenLock)
                {
                    if (IsOpen)
                        return;
                    IsOpen = true;
                }
                _ = Task.Run(() => ListeningThread(connection));
            }

            private async Task ListeningThread(IConnection connection)
            {
            retry:

                try
                {
                    if (this.channel is null)
                    {
                        this.channel = connection.CreateModel();
                        this.channel.BasicQos(0, (ushort)maxConcurrent, false);
                    }
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Direct);

                    //not auto-deleted so commands sent while no replica is connected, such as during a reconnect, wait in the queue
                    var queue = this.channel.QueueDeclare(this.topic, true, false, false);
                    this.channel.QueueBind(queue.QueueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        try
                        {
                            await throttle.WaitAsync(canceller.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }

                        if (!commandCounter.BeginReceive())
                            return; //don't receive anymore, externally will be shutdown

                        try
                        {
                            consumer.Model.BasicAck(e.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            log?.Error(topic, ex);
                            commandCounter.CancelReceive(throttle);
                            return;
                        }

                        var body = e.Body.ToArray();
                        var replyTo = e.BasicProperties.ReplyTo;
                        var correlationId = e.BasicProperties.CorrelationId;
                        var handleTask = Task.Run(() => HandleMessage(body, replyTo, correlationId));
                        _ = handling.Add(handleTask);
                        _ = handleTask.ContinueWith(removeHandling, handling, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                    };

                    consumer.ConsumerCancelled += (sender, e) =>
                    {
                        if (!canceller.IsCancellationRequested && consumer.Model.IsOpen)
                            _ = Task.Run(() => ListeningThread(connection));
                        return Task.CompletedTask;
                    };

                    this.consumerTag = this.channel.BasicConsume(queue.QueueName, false, consumer);
                }
                catch (Exception ex)
                {
                    if (!canceller.IsCancellationRequested)
                    {
                        log?.Error(topic, ex);

                        if (channel is not null)
                        {
                            channel.Close();
                            channel.Dispose();
                            channel = null;
                        }
                        await Task.Delay(RabbitMQCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            private async Task HandleMessage(byte[] body, string? replyTo, string? correlationId)
            {
                object? result = null;
                Exception? error = null;
                var awaitResponse = !String.IsNullOrWhiteSpace(replyTo);

                var inHandlerContext = false;
                try
                {
                    if (encryptor is not null)
                        body = encryptor.Decrypt(body);

                    var message = serializer.Deserialize<RabbitMQMessage>(body);
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
                    if (!inHandlerContext)
                        log?.Error(topic, ex);

                    error = ex;
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
                    var replyProperties = this.channel!.CreateBasicProperties();
                    replyProperties.CorrelationId = correlationId;

                    var acknowledgement = new Acknowledgement(serializer, result, error);

                    var acknowledgmentBody = serializer.SerializeBytes(acknowledgement);
                    if (encryptor is not null)
                        acknowledgmentBody = encryptor.Encrypt(acknowledgmentBody);

                    this.channel.BasicPublish(String.Empty, replyTo!, replyProperties, acknowledgmentBody);
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);
                }
                finally
                {
                    commandCounter.CompleteReceive(throttle);
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
                {
                    try
                    {
                        channel.BasicCancel(consumerTag);
                    }
                    catch (Exception ex)
                    {
                        log?.Error(topic, ex);
                    }
                }
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
                    channel.Close();
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
                    channel.Close();
                    channel.Dispose();
                    channel = null;
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}
