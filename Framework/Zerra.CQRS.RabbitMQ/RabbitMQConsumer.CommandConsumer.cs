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
            private readonly CancellationTokenSource canceller;

            private IModel? channel = null;
            private SemaphoreSlim? throttle = null;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
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
                this.canceller = new CancellationTokenSource();
            }

            public void Open(IConnection connection)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(connection, handlerAsync, handlerAwaitAsync);
            }

            private async Task ListeningThread(IConnection connection, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {

            retry:

                throttle?.Dispose();
                throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

                try
                {
                    if (this.channel != null)
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

                        var awaitResponse = !String.IsNullOrWhiteSpace(e.BasicProperties.ReplyTo);

                        Acknowledgement? acknowledgment;
                        if (awaitResponse)
                            acknowledgment = new Acknowledgement();
                        else
                            acknowledgment = null;

                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQCommandMessage? message;
                            if (symmetricConfig != null)
                                message = RabbitMQCommon.Deserialize<RabbitMQCommandMessage>(SymmetricEncryptor.Decrypt(symmetricConfig, e.Body.Span));
                            else
                                message = RabbitMQCommon.Deserialize<RabbitMQCommandMessage>(e.Body.Span);

                            if (message == null || message.Message == null || message.Source == null)
                                throw new Exception("Invalid Message");

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

                            if (acknowledgment != null)
                            {
                                acknowledgment.Success = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!inHandlerContext)
                                _ = Log.ErrorAsync(topic, ex);

                            if (acknowledgment != null)
                            {
                                acknowledgment.Success = false;
                                acknowledgment.ErrorMessage = ex.Message;
                            }
                        }
                        finally
                        {
                            if (acknowledgment == null)
                            {
                                commandCounter.CompleteReceive(throttle);
                            }
                        }

                        if (acknowledgment == null)
                            return;

                        try
                        {
                            var replyProperties = this.channel.CreateBasicProperties();
                            replyProperties.CorrelationId = e.BasicProperties.CorrelationId;

                            var acknowledgmentBody = RabbitMQCommon.Serialize(acknowledgment);
                            if (symmetricConfig != null)
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
                        _ = ListeningThread(connection, handlerAsync, handlerAwaitAsync);
                        return Task.CompletedTask;
                    };

                    _ = this.channel.BasicConsume(queue.QueueName, false, consumer);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(topic, ex);


                    if (!canceller.IsCancellationRequested)
                    {
                        if (channel != null)
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

                if (channel != null)
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
