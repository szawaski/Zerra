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
    public partial class KafkaServer
    {
        public class CommandConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricKey encryptionKey;
            private readonly CancellationTokenSource canceller;
            private readonly string topicAck;

            public CommandConsumer(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.topic = type.GetNiceName();
                this.encryptionKey = encryptionKey;
                this.canceller = new CancellationTokenSource();
                this.topicAck = $"{this.topic}-ACK";
            }

            public async Task Open(string host, Func<ICommand, Task> handlerAsync, Func<ICommand, Task> handlerAwaitAsync)
            {
                var consumerConfig = new ConsumerConfig();
                consumerConfig.BootstrapServers = host;
                consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
                consumerConfig.EnableAutoCommit = false;

                IConsumer<string, byte[]> consumer = null;
                try
                {
                    consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
                    consumer.Subscribe(topic);
                    for (; ; )
                    {
                        Exception error = null;
                        bool awaitResponse = false;
                        string ackKey = null;
                        try
                        {
                            if (canceller.Token.IsCancellationRequested)
                                break;

                            var consumerResult = consumer.Consume(canceller.Token);

                            awaitResponse = consumerResult.Message.Key == KafkaCommon.MessageWithAckKey;
                            if (awaitResponse)
                                ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));

                            if (consumerResult.Message.Key == KafkaCommon.MessageKey || awaitResponse)
                            {
                                var stopwatch = new Stopwatch();
                                stopwatch.Start();

                                byte[] body = consumerResult.Message.Value;
                                if (encryptionKey != null)
                                    body = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, body, true);

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

                                stopwatch.Stop();
                                _ = Log.TraceAsync($"Received Await: {topic}  {stopwatch.ElapsedMilliseconds}");
                            }
                            else
                            {
                                _ = Log.ErrorAsync($"{nameof(KafkaServer)} unrecognized message key {consumerResult.Message.Key}");
                            }

                            if (canceller.Token.IsCancellationRequested)
                                break;
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
                            IProducer<string, byte[]> producer = null;
                            try
                            {
                                var producerConfig = new ProducerConfig();
                                producerConfig.BootstrapServers = host;
                                producerConfig.ClientId = Guid.NewGuid().ToString();

                                producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

                                var ack = new Acknowledgement()
                                {
                                    Success = error == null,
                                    ErrorMessage = error.Message
                                };
                                var body = KafkaCommon.Serialize(ack);
                                if (encryptionKey != null)
                                    body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body, true);

                                await producer.ProduceAsync(topicAck, new Message<string, byte[]>() { Key = ackKey, Value = body });
                            }

                            catch (Exception ex)
                            {
                                _ = Log.ErrorAsync(ex);
                            }
                            finally
                            {
                                if (producer != null)
                                    producer.Dispose();
                            }
                        }
                    }

                    consumer.Unsubscribe();
                }
                finally
                {
                    canceller.Dispose();
                    if (consumer != null)
                        consumer.Dispose();
                }
            }

            public void Dispose()
            {
                canceller.Cancel();
            }
        }
    }
}
