// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        public sealed class EventConsumer : IDisposable
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

            public EventConsumer(int maxConcurrent, int? maxReceive, Action processExit, string topic, SymmetricConfig symmetricConfig, string environment, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));
                if (maxReceive.HasValue && maxReceive.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(maxReceive));

                this.maxConcurrent = maxReceive.HasValue ? Math.Min(maxReceive.Value, maxConcurrent) : maxConcurrent;
                this.maxReceive = maxReceive;
                this.processExit = processExit;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{topic}".Truncate(KafkaCommon.TopicMaxLength);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(host, handlerAsync);
            }

            public async Task ListeningThread(string host, HandleRemoteEventDispatch handlerAsync)
            {
                using var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await KafkaCommon.EnsureTopic(host, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = host;
                    consumerConfig.GroupId = Guid.NewGuid().ToString("N");
                    consumerConfig.EnableAutoCommit = false;

                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                    {
                        consumer.Subscribe(topic);

                        try
                        {
                            for (; ; )
                            {
                                await throttle.WaitAsync();

                                if (maxReceive.HasValue)
                                {
                                    lock (countLocker)
                                    {
                                        if (receivedCount == maxReceive.Value)
                                            continue; //fill throttle, don't receive anymore, externally will be shutdown (shouldn't hit this line)
                                        receivedCount++;
                                    }
                                }

                                var consumerResult = consumer.Consume(canceller.Token);
                                consumer.Commit(consumerResult);

                                _ = HandleMessage(throttle, host, consumerResult, handlerAsync);

                                if (canceller.IsCancellationRequested)
                                    break;
                            }
                        }
                        finally
                        {
                            consumer.Unsubscribe();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(topic, ex);
                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, string host, ConsumeResult<string, byte[]> consumerResult, HandleRemoteEventDispatch handlerAsync)
            {
                var inHandlerContext = false;
                try
                {
                    if (consumerResult.Message.Key == KafkaCommon.MessageKey)
                    {
                        var body = consumerResult.Message.Value;
                        if (symmetricConfig != null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                        var message = KafkaCommon.Deserialize<KafkaEventMessage>(body);

                        if (message.Claims != null)
                        {
                            var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }

                        inHandlerContext = true;
                        await handlerAsync(message.Message, message.Source, false);
                        inHandlerContext = false;
                    }
                    else
                    {
                        _ = Log.ErrorAsync($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    if (inHandlerContext)
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
            }

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();
            }
        }
    }
}
