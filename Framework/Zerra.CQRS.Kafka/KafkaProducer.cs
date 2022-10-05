// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public class KafkaProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private bool listenerStarted = false;
        private SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly string host;
        private readonly SymmetricConfig symmetricConfig;
        private readonly string environment;
        private readonly string ackTopic;
        private readonly IProducer<string, byte[]> producer;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public KafkaProducer(string host, SymmetricConfig symmetricConfig, string environment)
        {
            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;

            var clientID = Guid.NewGuid().ToString("N");
            this.ackTopic = $"ACK-{clientID}";

            var producerConfig = new ProducerConfig();
            producerConfig.BootstrapServers = host;
            producerConfig.ClientId = clientID;
            producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
        }

        public string ConnectionString => host;

        Task ICommandProducer.DispatchAsync(ICommand command) { return SendAsync(command, false); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command) { return SendAsync(command, true); }
        Task IEventProducer.DispatchAsync(IEvent @event) { return SendAsync(@event); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement)
        {
            if (requireAcknowledgement)
            {
                await listenerStartedLock.WaitAsync();
                try
                {
                    if (!listenerStarted)
                    {
                        await KafkaCommon.EnsureTopic(host, ackTopic);
                        _ = Task.Run(AckListeningThread);
                        listenerStarted = true;
                    }
                }
                finally
                {
                    _ = listenerStartedLock.Release();
                }
            }

            string topic;
            if (!String.IsNullOrWhiteSpace(environment))
                topic = $"{environment}_{command.GetType().GetNiceName()}".Truncate(KafkaCommon.TopicMaxLength);
            else
                topic = command.GetType().GetNiceName().Truncate(KafkaCommon.TopicMaxLength);

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new KafkaCommandMessage()
            {
                Message = command,
                Claims = claims
            };

            var body = KafkaCommon.Serialize(message);
            if (symmetricConfig != null)
                body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

            if (requireAcknowledgement)
            {
                var ackKey = Guid.NewGuid().ToString("N");

                var headers = new Headers();
                headers.Add(new Header(KafkaCommon.AckTopicHeader, Encoding.UTF8.GetBytes(ackTopic)));
                headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                var key = KafkaCommon.MessageWithAckKey;

                var waiter = new SemaphoreSlim(0, 1);

                try
                {
                    Acknowledgement ack = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        ack = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body });
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                    await waiter.WaitAsync();

                    if (!ack.Success)
                        throw new AcknowledgementException(ack, topic);
                }
                finally
                {
                    _ = ackCallbacks.TryRemove(ackKey, out _);
                    if (waiter != null)
                        waiter.Dispose();
                }
            }
            else
            {
                var key = KafkaCommon.MessageKey;

                var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = key, Value = body });
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            string topic;
            if (!String.IsNullOrWhiteSpace(environment))
                topic = $"{environment}_{@event.GetType().GetNiceName()}".Truncate(KafkaCommon.TopicMaxLength);
            else
                topic = @event.GetType().GetNiceName().Truncate(KafkaCommon.TopicMaxLength);

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new KafkaEventMessage()
            {
                Message = @event,
                Claims = claims
            };

            var body = KafkaCommon.Serialize(message);
            if (symmetricConfig != null)
                body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

            var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = KafkaCommon.MessageKey, Value = body });
            if (producerResult.Status != PersistenceStatus.Persisted)
                throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");
        }

        private async Task AckListeningThread()
        {
            var consumerConfig = new ConsumerConfig();
            consumerConfig.BootstrapServers = host;
            consumerConfig.GroupId = ackTopic;
            consumerConfig.EnableAutoCommit = false;

        retry:

            try
            {
                using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                {
                    consumer.Subscribe(ackTopic);
                    for (; ; )
                    {
                        try
                        {
                            var consumerResult = consumer.Consume(canceller.Token);
                            consumer.Commit(consumerResult);

                            if (!ackCallbacks.TryRemove(consumerResult.Message.Key, out var callback))
                                continue;

                            var response = consumerResult.Message.Value;
                            if (symmetricConfig != null)
                                response = SymmetricEncryptor.Decrypt(symmetricConfig, response);
                            var ack = KafkaCommon.Deserialize<Acknowledgement>(response);

                            callback(ack);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch { }
                    }

                    consumer.Unsubscribe();
                }
                await KafkaCommon.DeleteTopic(host, ackTopic);
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
            finally
            {
                canceller.Dispose();
            }
        }

        public void Dispose()
        {
            canceller.Cancel();
            producer.Dispose();
            listenerStartedLock.Dispose();
        }
    }
}
