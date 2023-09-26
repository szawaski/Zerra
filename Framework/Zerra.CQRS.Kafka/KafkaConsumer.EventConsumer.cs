﻿// Copyright © KaKush LLC
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
            private readonly ReceiveCounter receiveCounter;
            private readonly string topic;
            private readonly SymmetricConfig symmetricConfig;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;

            public EventConsumer(int maxConcurrent, ReceiveCounter receiveCounter, string topic, SymmetricConfig symmetricConfig, string environment, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = receiveCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(receiveCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.receiveCounter = receiveCounter;

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

                                if (!receiveCounter.BeginReceive())
                                    continue; //don't receive anymore, externally will be shutdown, fill throttle

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
                    receiveCounter.CompleteReceive(throttle);
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
