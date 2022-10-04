// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public partial class KafkaConsumer
    {
        public class CommandConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly string clientID;
            private readonly SymmetricConfig symmetricConfig;

            private CancellationTokenSource canceller = null;

            public CommandConsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(KafkaCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(KafkaCommon.TopicMaxLength);
                this.clientID = Guid.NewGuid().ToString();
                this.symmetricConfig = symmetricConfig;
            }

            public void Open(string host, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, handlerAsync, handlerAwaitAsync));
            }

            private async Task ListeningThread(string host, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                canceller = new CancellationTokenSource();

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

                        for (; ; )
                        {
                            Exception error = null;
                            var awaitResponse = false;
                            string ackTopic = null;
                            string ackKey = null;
                            try
                            {
                                var consumerResult = consumer.Consume(canceller.Token);
                                consumer.Commit(consumerResult);

                                awaitResponse = consumerResult.Message.Key == KafkaCommon.MessageWithAckKey;
                                if (awaitResponse)
                                {
                                    ackTopic = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckTopicHeader));
                                    ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));
                                }

                                if (consumerResult.Message.Key == KafkaCommon.MessageKey || awaitResponse)
                                {
                                    var stopwatch = Stopwatch.StartNew();

                                    var body = consumerResult.Message.Value;
                                    if (symmetricConfig != null)
                                        body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                                    var message = KafkaCommon.Deserialize<KafkaCommandMessage>(body);

                                    if (message.Claims != null)
                                    {
                                        var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                        Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                                    }

                                    if (awaitResponse)
                                        await handlerAwaitAsync(message.Message);
                                    else
                                        await handlerAsync(message.Message);

                                    _ = Log.TraceAsync($"Received Await: {topic} {stopwatch.ElapsedMilliseconds}");
                                }
                                else
                                {
                                    _ = Log.ErrorAsync($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                break;
                            }
                            catch (Exception ex)
                            {
                                _ = Log.TraceAsync($"Error: Received Await: {topic}");
                                _ = Log.ErrorAsync(ex);
                                error = ex;
                            }
                            if (awaitResponse)
                            {
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
                                        _ = await producer.ProduceAsync(ackTopic, new Message<string, byte[]>() { Key = ackKey, Value = body });
                                    }
                                }

                                catch (Exception ex)
                                {
                                    _ = Log.ErrorAsync(ex);
                                }
                            }
                        }

                        consumer.Unsubscribe();
                    }
                }
                catch (Exception ex)
                {
                    if (!canceller.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(ex);
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
                canceller.Dispose();
                IsOpen = false;
            }

            public void Dispose()
            {
                if (canceller != null)
                    canceller.Cancel();
            }
        }
    }
}
