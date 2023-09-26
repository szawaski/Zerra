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
        private sealed class EventConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly int? maxReceive;
            private readonly Action processExit;
            private readonly string topic;
            private readonly SymmetricConfig symmetricConfig;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;

            private readonly object countLocker = new();
            private int receivedCount;
            private int completedCount;

            private IModel channel = null;
            private SemaphoreSlim throttle = null;

            public EventConsumer(int maxConcurrent, int? maxReceive, Action processExit, string topic, SymmetricConfig symmetricConfig, string environment, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));
                if (maxReceive.HasValue && maxReceive.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(maxReceive));

                this.maxConcurrent = maxReceive.HasValue ? Math.Min(maxReceive.Value, maxConcurrent) : maxConcurrent;
                this.maxReceive = maxReceive;
                this.processExit = processExit;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{topic}".Truncate(RabbitMQCommon.TopicMaxLength);
                else
                    this.topic = topic.Truncate(RabbitMQCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(IConnection connection)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(connection);
            }

            private async Task ListeningThread(IConnection connection)
            {
                throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    if (this.channel != null)
                        throw new Exception("Exchange already open");

                    this.channel = connection.CreateModel();
                    this.channel.ExchangeDeclare(this.topic, ExchangeType.Fanout);

                    var queueName = this.channel.QueueDeclare().QueueName;
                    this.channel.QueueBind(queueName, this.topic, String.Empty);

                    var consumer = new AsyncEventingBasicConsumer(this.channel);

                    consumer.Received += async (sender, e) =>
                    {
                        await throttle.WaitAsync();

                        if (maxReceive.HasValue)
                        {
                            lock (countLocker)
                            {
                                receivedCount++;
                            }
                        }

                        var inHandlerContext = false;
                        try
                        {
                            RabbitMQEventMessage message;
                            if (symmetricConfig != null)
                                message = RabbitMQCommon.Deserialize<RabbitMQEventMessage>(SymmetricEncryptor.Decrypt(symmetricConfig, e.Body.Span));
                            else
                                message = RabbitMQCommon.Deserialize<RabbitMQEventMessage>(e.Body.Span);

                            if (message.Claims != null)
                            {
                                var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                            }

                            inHandlerContext = true;
                            await handlerAsync(message.Message, message.Source, false);
                            inHandlerContext = false;
                        }
                        catch (Exception ex)
                        {
                            if (!inHandlerContext)
                                _ = Log.ErrorAsync(topic, ex);
                        }
                        finally
                        {
                            if (maxReceive.HasValue)
                            {
                                lock (countLocker)
                                {
                                    completedCount++;
                                    if (completedCount == maxReceive.Value)
                                        processExit?.Invoke();
                                    else if (throttle.CurrentCount < maxReceive.Value - receivedCount)
                                        _ = throttle.Release(); //don't release more than needed to reach maxReceive
                                }
                            }
                            else
                            {
                                _ = throttle.Release();
                            }
                        }
                    };

                    _ = this.channel.BasicConsume(queueName, true, consumer);
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

                if (throttle != null)
                    throttle.Dispose();

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
