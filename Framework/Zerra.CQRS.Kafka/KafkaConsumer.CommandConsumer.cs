// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        public sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly ReceiveCounter receiveCounter;
            private readonly string topic;
            private readonly string clientID;
            private readonly SymmetricConfig symmetricConfig;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly CancellationTokenSource canceller;

            private readonly object countLocker = new();
            private int receivedCount;
            private int completedCount;

            public CommandConsumer(int maxConcurrent, ReceiveCounter receiveCounter, string topic, SymmetricConfig symmetricConfig, string environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = receiveCounter.MaxReceive.HasValue ? Math.Min(receiveCounter.MaxReceive.Value, maxConcurrent) : maxConcurrent;
                this.receiveCounter = receiveCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{topic}".Truncate(KafkaCommon.TopicMaxLength);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength);
                this.clientID = Guid.NewGuid().ToString("N");
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = ListeningThread(host, handlerAsync, handlerAwaitAsync);
            }

            private async Task ListeningThread(string host, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                using var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await KafkaCommon.EnsureTopic(host, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = host;
                    consumerConfig.GroupId = topic;
                    consumerConfig.EnableAutoCommit = false;

                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                    {
                        consumer.Subscribe(topic);
                        try
                        {
                            for (; ; )
                            {
                                await throttle.WaitAsync();

                                if (!receiveCounter.BeginReceived())
                                    continue; //fill throttle, don't receive anymore, externally will be shutdown

                                var consumerResult = consumer.Consume(canceller.Token);
                                consumer.Commit(consumerResult);

                                _ = HandleMessage(throttle, host, consumerResult, handlerAsync, handlerAwaitAsync);

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

                canceller.Dispose();
            }

            private async Task HandleMessage(SemaphoreSlim throttle, string host, ConsumeResult<string, byte[]> consumerResult, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync)
            {
                Exception error = null;
                var awaitResponse = false;
                string ackTopic = null;
                string ackKey = null;

                var inHandlerContext = false;
                try
                {
                    awaitResponse = consumerResult.Message.Key == KafkaCommon.MessageWithAckKey;
                    if (awaitResponse)
                    {
                        ackTopic = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckTopicHeader));
                        ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));
                    }

                    if (consumerResult.Message.Key == KafkaCommon.MessageKey || awaitResponse)
                    {
                        var body = consumerResult.Message.Value;
                        if (symmetricConfig != null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                        var message = KafkaCommon.Deserialize<KafkaCommandMessage>(body);

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
                    }
                    else
                    {
                        _ = Log.ErrorAsync($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(topic, ex);
                }
                finally
                {
                    if (!awaitResponse)
                    {
                        receiveCounter.CompleteReceive(throttle);
                    }
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ack = new Acknowledgement()
                    {
                        Success = error == null,
                        ErrorMessage = error?.Message
                    };
                    var body = KafkaCommon.Serialize(ack);
                    if (symmetricConfig != null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    var producerConfig = new ProducerConfig();
                    producerConfig.BootstrapServers = host;
                    producerConfig.ClientId = clientID;
                    using (var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build())
                    {
                        _ = await producer.ProduceAsync(ackTopic, new Message<string, byte[]>()
                        {
                            Key = ackKey,
                            Value = body
                        });
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
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
