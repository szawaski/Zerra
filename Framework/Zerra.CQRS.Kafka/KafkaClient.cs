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

namespace Zerra.CQRS.Kafka
{
    //Kafka Producer
    public class KafkaClient : ICommandClient, IEventClient, IDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.RijndaelManaged;

        private bool listenerStarted = false;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;
        private readonly string ackTopic;
        private static IProducer<string, byte[]> producer;
        private readonly SemaphoreSlim listenerStartedLock;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public KafkaClient(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;

            var clientID = Guid.NewGuid().ToString();
            this.ackTopic = $"ACK-{clientID}";

            var producerConfig = new ProducerConfig();
            producerConfig.BootstrapServers = host;
            producerConfig.ClientId = clientID;
            producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

            this.listenerStartedLock = new SemaphoreSlim(1, 1);
            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
        }

        public string ConnectionString => host;

        Task ICommandClient.DispatchAsync(ICommand command) { return SendAsync(command, false); }
        Task ICommandClient.DispatchAsyncAwait(ICommand command) { return SendAsync(command, true); }
        Task IEventClient.DispatchAsync(IEvent @event) { return SendAsync(@event); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement)
        {
            if (requireAcknowledgement)
            {
                if (!listenerStarted)
                {
                    await listenerStartedLock.WaitAsync();
                    if (!listenerStarted)
                    {
                        _ = Task.Run(AckListeningThread);
                        listenerStarted = true;
                    }
                    listenerStartedLock.Release();
                }
            }

            var topic = command.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new KafkaCommandMessage()
            {
                Message = command,
                Claims = claims
            };

            var body = KafkaCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body, true);

            if (requireAcknowledgement)
            {
                var ackKey = Guid.NewGuid().ToString();

                var headers = new Headers();
                headers.Add(new Header(KafkaCommon.AckTopicHeader, Encoding.UTF8.GetBytes(ackTopic)));
                headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                var key = KafkaCommon.MessageWithAckKey;

                var waiter = new SemaphoreSlim(0, 1);

                try
                {
                    Acknowledgement ack = null;
                    ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        ack = ackFromCallback;
                        waiter.Release();
                    });

                    var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body });
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(KafkaClient)} failed: {producerResult.Status}");

                    await waiter.WaitAsync();
                    if (!ack.Success)
                        throw new AcknowledgementException(ack, topic);
                }
                finally
                {
                    if (waiter != null)
                        waiter.Dispose();
                }
            }
            else
            {
                var key = KafkaCommon.MessageKey;

                var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = key, Value = body });
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(KafkaClient)} failed: {producerResult.Status}");
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            var topic = @event.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new KafkaEventMessage()
            {
                Message = @event,
                Claims = claims
            };

            var body = KafkaCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body, true);

            var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = KafkaCommon.MessageKey, Value = body });
            if (producerResult.Status != PersistenceStatus.Persisted)
                throw new Exception($"{nameof(KafkaClient)} failed: {producerResult.Status}");
        }

        private async Task AckListeningThread()
        {
            await KafkaCommon.AssureTopic(host, ackTopic);

            var consumerConfig = new ConsumerConfig();
            consumerConfig.BootstrapServers = host;
            consumerConfig.GroupId = ackTopic;
            consumerConfig.EnableAutoCommit = false;

            IConsumer<string, byte[]> consumer = null;
            try
            {
                consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
                consumer.Subscribe(ackTopic);
                for (; ; )
                {
                    try
                    {
                        if (canceller.Token.IsCancellationRequested)
                            break;

                        var consumerResult = consumer.Consume(canceller.Token);
                        consumer.Commit(consumerResult);

                        if (!ackCallbacks.TryRemove(consumerResult.Message.Key, out Action<Acknowledgement> callback))
                            continue;

                        var response = consumerResult.Message.Value;
                        if (encryptionKey != null)
                            response = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, response, true);
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
                await KafkaCommon.DeleteTopic(host, ackTopic);
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
            listenerStartedLock.Dispose();
            canceller.Cancel();
            producer.Dispose();
        }
    }
}
