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
    public partial class KafkaConsumer
    {
        public class EventConsumer : IDisposable
        {
            public Type Type { get; private set; }
            public bool IsOpen { get; private set; }

            private readonly string topic;
            private readonly SymmetricKey encryptionKey;
            private CancellationTokenSource canceller;

            public EventConsumer(Type type, SymmetricKey encryptionKey)
            {
                this.Type = type;
                this.topic = type.GetNiceName();
                this.encryptionKey = encryptionKey;
            }

            public void Open(string host, Func<IEvent, Task> handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                Task.Run(() => ListeningThread(host, handlerAsync));
            }

            public async Task ListeningThread(string host, Func<IEvent, Task> handlerAsync)
            {
                canceller = new CancellationTokenSource();

            retry:

                IConsumer<string, byte[]> consumer = null;
                try
                {
                    await KafkaCommon.AssureTopic(host, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = host;
                    consumerConfig.GroupId = Guid.NewGuid().ToString();
                    consumerConfig.EnableAutoCommit = false;

                    consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
                    consumer.Subscribe(topic);

                    for (; ; )
                    {
                        try
                        {
                            if (canceller.Token.IsCancellationRequested)
                                break;

                            var consumerResult = consumer.Consume(canceller.Token);
                            consumer.Commit(consumerResult);

                            if (consumerResult.Message.Key == KafkaCommon.MessageKey)
                            {
                                var stopwatch = new Stopwatch();
                                stopwatch.Start();

                                byte[] body = consumerResult.Message.Value;
                                if (encryptionKey != null)
                                    body = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, body);

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
                        }
                    }

                    consumer.Unsubscribe();
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);

                    if (consumer != null)
                        consumer.Dispose();
                    consumer = null;

                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(retryDelay);
                        goto retry;
                    }
                }
                canceller.Dispose();
                canceller = null;
                if (consumer != null)
                    consumer.Dispose();
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
