// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public partial class KafkaServer
    {
        public class EventConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricKey encryptionKey;
            private readonly CancellationTokenSource canceller;

            public EventConsumer(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.topic = type.GetNiceName();
                this.encryptionKey = encryptionKey;
                this.canceller = new CancellationTokenSource();
            }

            public async Task Open(string host, Func<IEvent, Task> handlerAsync)
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
                        try
                        {
                            if (canceller.Token.IsCancellationRequested)
                                break;

                            var consumerResult = consumer.Consume(canceller.Token);

                            if (consumerResult.Message.Key == KafkaCommon.MessageKey)
                            {
                                var stopwatch = new Stopwatch();
                                stopwatch.Start();

                                byte[] body = consumerResult.Message.Value;
                                if (encryptionKey != null)
                                    body = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, body, true);

                                var message = KafkaCommon.Deserialize<KafkaEventMessage>(body);

                                if (message.Claims != null)
                                {
                                    var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                                    Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                                }

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
