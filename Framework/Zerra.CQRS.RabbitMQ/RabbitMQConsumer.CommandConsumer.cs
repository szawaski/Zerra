// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Security.Claims;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;
using Zerra.Serialization;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed partial class RabbitMQConsumer
    {
        private sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter commandCounter;
            private readonly string topic;
            private readonly ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILog? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
            private readonly object isOpenLock = new object();

            private IModel? channel = null;
            private SemaphoreSlim? throttle = null;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, ISerializer serializer, IEncryptor? encryptor, ILog? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
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

                throttle?.Dispose();
                throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

                try
                {
                    if (this.channel is not null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.BasicQos(0, (ushort)maxConcurrent, false);
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Direct);

                    var queue = this.channel.QueueDeclare(String.Empty, false, true, true);
                    this.channel.QueueBind(queue.QueueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        await throttle.WaitAsync(canceller.Token);

                        if (!commandCounter.BeginReceive())
                            return; //don't receive anymore, externally will be shutdown

                        this.channel.BasicAck(e.DeliveryTag, false);

                        object? result = null;
                        Exception? error = null;
                        var awaitResponse = !String.IsNullOrWhiteSpace(e.BasicProperties.ReplyTo);

                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQMessage? message;
                            if (encryptor is not null)
                                message = serializer.Deserialize<RabbitMQMessage>(encryptor.Decrypt(e.Body.Span));
                            else
                                message = serializer.Deserialize<RabbitMQMessage>(e.Body.Span);

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
                            var replyProperties = this.channel.CreateBasicProperties();
                            replyProperties.CorrelationId = e.BasicProperties.CorrelationId;

                            var acknowledgement = new Acknowledgement(result, error);

                            var acknowledgmentBody = serializer.SerializeBytes(acknowledgement);
                            if (encryptor is not null)
                                acknowledgmentBody = encryptor.Encrypt(acknowledgmentBody);

                            this.channel.BasicPublish(String.Empty, e.BasicProperties.ReplyTo, replyProperties, acknowledgmentBody);
                        }
                        catch (Exception ex)
                        {
                            log?.Error(topic, ex);
                        }
                        finally
                        {
                            commandCounter.CompleteReceive(throttle);
                        }
                    };

                    consumer.ConsumerCancelled += (sender, e) =>
                    {
                        _ = Task.Run(() => ListeningThread(connection));
                        return Task.CompletedTask;
                    };

                    _ = this.channel.BasicConsume(queue.QueueName, false, consumer);
                }
                catch (Exception ex)
                {
                    log?.Error(topic, ex);

                    if (!canceller.IsCancellationRequested)
                    {
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

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();

                throttle?.Dispose();

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
