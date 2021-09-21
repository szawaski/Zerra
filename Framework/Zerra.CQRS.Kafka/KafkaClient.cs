// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Collections.Generic;
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

        private readonly string host;
        private readonly SymmetricKey encryptionKey;
        public KafkaClient(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;
        }

        public string ConnectionString => host;

        Task ICommandClient.DispatchAsync(ICommand command) { return SendAsync(command, false); }
        Task ICommandClient.DispatchAsyncAwait(ICommand command) { return SendAsync(command, true); }
        Task IEventClient.DispatchAsync(IEvent @event) { return SendAsync(@event); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement)
        {
            IProducer<string, byte[]> producer = null;
            IConsumer<string, byte[]> consumer = null;
            try
            {
                var producerConfig = new ProducerConfig();
                producerConfig.BootstrapServers = host;
                producerConfig.ClientId = Guid.NewGuid().ToString();
                producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

                var topic = command.GetType().GetNiceName();
                var ackKey = $"{KafkaCommon.MessageWithAckKey}-{producerConfig.ClientId}";

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

                Headers headers;
                string key;
                if (requireAcknowledgement)
                {
                    headers = new Headers();
                    headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                    key = KafkaCommon.MessageWithAckKey;
                }
                else
                {
                    headers = null;
                    key = KafkaCommon.MessageKey;
                }

                var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body });
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(KafkaClient)} failed: {producerResult.Status}");

                if (requireAcknowledgement)
                {
                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = host;
                    consumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
                    consumerConfig.EnableAutoCommit = false;
                    consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();

                    var topicAck = $"{topic}-ACK";
                    consumer.Subscribe(topicAck);
                    for (; ; )
                    {
                        var consumerResult = consumer.Consume();
                        if (consumerResult.Message.Key == ackKey)
                        {
                            var response = consumerResult.Message.Value;
                            if (encryptionKey != null)
                                response = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, response, true);
                            var ack = KafkaCommon.Deserialize<Acknowledgement>(response);
                            if (ack.Success)
                                break;
                            throw new AcknowledgementException(ack, topic);
                        }
                    }
                    consumer.Unsubscribe();
                }
            }
            finally
            {
                if (producer != null)
                    producer.Dispose();
                if (consumer != null)
                    consumer.Dispose();
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            IProducer<string, byte[]> producer = null;
            try
            {
                var producerConfig = new ProducerConfig();
                producerConfig.BootstrapServers = host;
                producerConfig.ClientId = Guid.NewGuid().ToString();
                producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

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
            finally
            {
                if (producer != null)
                    producer.Dispose();
            }
        }

        public void Dispose()
        {

        }
    }
}
