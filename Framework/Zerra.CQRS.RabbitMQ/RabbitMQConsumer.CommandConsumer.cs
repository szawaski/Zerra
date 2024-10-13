// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

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
            private readonly SymmetricConfig? symmetricConfig;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;
            private readonly object isOpenLock = new object();

            private IModel? channel = null;
            private SemaphoreSlim? throttle = null;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
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
                _ = ListeningThread(connection);
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
                            if (symmetricConfig is not null)
                                message = RabbitMQCommon.Deserialize<RabbitMQMessage>(SymmetricEncryptor.Decrypt(symmetricConfig, e.Body.Span));
                            else
                                message = RabbitMQCommon.Deserialize<RabbitMQMessage>(e.Body.Span);

                            if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                                throw new Exception("Invalid Message");

                            var command = RabbitMQCommon.Deserialize(message.MessageType, message.MessageData) as ICommand;
                            if (command is null)
                                throw new Exception("Invalid Message");

                            if (message.Claims is not null)
                            {
                                var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            inHandlerContext = true;
                            if (message.HasResult)
                                result = await handlerWithResultAwaitAsync(command, message.Source, false);
                            else if (awaitResponse)
                                await handlerAwaitAsync(command, message.Source, false);
                            else
                                await handlerAsync(command, message.Source, false);
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
                                commandCounter.CompleteReceive(throttle);
                        }

                        if (!awaitResponse)
                            return;

                        try
                        {
                            var replyProperties = this.channel.CreateBasicProperties();
                            replyProperties.CorrelationId = e.BasicProperties.CorrelationId;

                            var acknowledgement = new Acknowledgement(result, error);

                            var acknowledgmentBody = RabbitMQCommon.Serialize(acknowledgement);
                            if (symmetricConfig is not null)
                                acknowledgmentBody = SymmetricEncryptor.Encrypt(symmetricConfig, acknowledgmentBody);

                            this.channel.BasicPublish(String.Empty, e.BasicProperties.ReplyTo, replyProperties, acknowledgmentBody);
                        }
                        catch (Exception ex)
                        {
                            _ = Log.ErrorAsync(topic, ex);
                        }
                        finally
                        {
                            commandCounter.CompleteReceive(throttle);
                        }
                    };

                    consumer.ConsumerCancelled += (sender, e) =>
                    {
                        _ = ListeningThread(connection);
                        return Task.CompletedTask;
                    };

                    _ = this.channel.BasicConsume(queue.QueueName, false, consumer);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(topic, ex);

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
