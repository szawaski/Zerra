// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Azure;
using Azure.Messaging.EventGrid;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.AzureEventGrid
{
    public class AzureEventGridProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private const SymmetricAlgorithmType encryptionAlgorithm = SymmetricAlgorithmType.AESwithShift;

        private bool listenerStarted = false;

        private readonly string host;
        private readonly SymmetricKey encryptionKey;
        private readonly string ackTopic;
        private readonly EventGridPublisherClient publisher;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public AzureEventGridProducer(string host, SymmetricKey encryptionKey)
        {
            this.host = host;
            this.encryptionKey = encryptionKey;

            var clientID = Guid.NewGuid().ToString();
            this.ackTopic = $"ACK-{clientID}";

            publisher = new EventGridPublisherClient(new Uri("<endpoint>"), new AzureKeyCredential("<access-key>"));

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
                lock (this)
                {
                    if (!listenerStarted)
                    {
                        _ = Task.Run(AckListeningThread);
                        listenerStarted = true;
                    }
                }
            }

            var partition = command.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureEventGridCommandMessage()
            {
                Message = command,
                Claims = claims
            };

            var body = AzureEventGridCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            if (requireAcknowledgement)
            {
                var ackKey = Guid.NewGuid().ToString();

                var headers = new Headers();
                headers.Add(new Header(AzureEventGridCommon.AckTopicHeader, Encoding.UTF8.GetBytes(ackTopic)));
                headers.Add(new Header(AzureEventGridCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                var key = AzureEventGridCommon.MessageWithAckKey;

                var waiter = new SemaphoreSlim(0, 1);

                try
                {
                    Acknowledgement ack = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        ack = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var producerResult = await producer.ProduceAsync(partition, new Message<string, byte[]> { Headers = headers, Key = key, Value = body });
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(AzureEventGridProducer)} failed: {producerResult.Status}");

                    await waiter.WaitAsync();
                    if (!ack.Success)
                        throw new AcknowledgementException(ack, partition);
                }
                finally
                {
                    if (waiter != null)
                        waiter.Dispose();
                }
            }
            else
            {
                var key = AzureEventGridCommon.MessageKey;

                var producerResult = await producer.ProduceAsync(partition, new Message<string, byte[]> { Key = key, Value = body });
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(AzureEventGridProducer)} failed: {producerResult.Status}");
            }
        }

        private async Task SendAsync(IEvent @event)
        {
            var topic = @event.GetType().GetNiceName();

            string[][] claims = null;
            if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

            var message = new AzureEventGridEventMessage()
            {
                Message = @event,
                Claims = claims
            };

            var body = AzureEventGridCommon.Serialize(message);
            if (encryptionKey != null)
                body = SymmetricEncryptor.Encrypt(encryptionAlgorithm, encryptionKey, body);

            await using (var producer = new EventGridPublisherClient(host, eventHubName))
            {

            }
                var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = AzureEventGridCommon.MessageKey, Value = body });
            if (producerResult.Status != PersistenceStatus.Persisted)
                throw new Exception($"{nameof(AzureEventGridProducer)} failed: {producerResult.Status}");
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
                await AzureEventGridCommon.EnsureTopic(host, ackTopic);

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
                            if (encryptionKey != null)
                                response = SymmetricEncryptor.Decrypt(encryptionAlgorithm, encryptionKey, response);
                            var ack = AzureEventGridCommon.Deserialize<Acknowledgement>(response);

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
                await AzureEventGridCommon.DeleteTopic(host, ackTopic);
            }
            catch (Exception ex)
            {
                if (!canceller.IsCancellationRequested)
                {
                    _ = Log.ErrorAsync(ex);
                    await Task.Delay(AzureEventGridCommon.RetryDelay);
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
        }
    }
}
