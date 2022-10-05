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
            private readonly SymmetricConfig symmetricConfig;
            private CancellationTokenSource canceller;

            public EventConsumer(Type type, SymmetricConfig symmetricConfig, string environment)
            {
                this.Type = type;
                this.Type = type;
                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = $"{environment}_{type.GetNiceName()}".Truncate(KafkaCommon.TopicMaxLength);
                else
                    this.topic = type.GetNiceName().Truncate(KafkaCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
            }

            public void Open(string host, Func<IEvent, Task> handlerAsync)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, handlerAsync));
            }

            public async Task ListeningThread(string host, Func<IEvent, Task> handlerAsync)
            {
                canceller = new CancellationTokenSource();

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
                                var consumerResult = consumer.Consume(canceller.Token);
                                consumer.Commit(consumerResult);

                                _ = Log.TraceAsync($"Received: {topic}");

                                _ = HandleMessage(host, consumerResult, handlerAsync);
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
                    _ = Log.ErrorAsync($"Error: {topic}", ex);
                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
                canceller.Dispose();
                IsOpen = false;
            }

            private async Task HandleMessage(string host, ConsumeResult<string, byte[]> consumerResult, Func<IEvent, Task> handlerAsync)
            {
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

                        await handlerAsync(message.Message);
                    }
                    else
                    {
                        _ = Log.ErrorAsync($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync($"Error: {topic}", ex);
                }
            }

            public void Dispose()
            {
                if (canceller != null)
                    canceller.Cancel();
            }
        }
    }
}
