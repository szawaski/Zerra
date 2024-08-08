// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.Kafka
{
    public sealed class KafkaProducer : ICommandProducer, IEventProducer, IDisposable
    {
        private bool listenerStarted = false;
        private readonly SemaphoreSlim listenerStartedLock = new(1, 1);

        private readonly string host;
        private readonly SymmetricConfig? symmetricConfig;
        private readonly string? environment;
        private readonly string ackTopic;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private readonly IProducer<string, byte[]> producer;
        private readonly CancellationTokenSource canceller;
        private readonly ConcurrentDictionary<string, Action<Acknowledgement>> ackCallbacks;
        public KafkaProducer(string host, SymmetricConfig? symmetricConfig, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.host = host;
            this.symmetricConfig = symmetricConfig;
            this.environment = environment;

            var clientID = StringExtensions.Join(KafkaCommon.TopicMaxLength - 4, "_", environment ?? "Unknown_Environment", Environment.MachineName, Config.EntryAssemblyName ?? "Unknown_Assembly");
            this.ackTopic = $"ACK-{clientID}";
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();

            var producerConfig = new ProducerConfig();
            producerConfig.BootstrapServers = host;
            producerConfig.ClientId = clientID;
            producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();

            this.canceller = new CancellationTokenSource();
            this.ackCallbacks = new ConcurrentDictionary<string, Action<Acknowledgement>>();
        }

        public string ConnectionString => host;

        Task ICommandProducer.DispatchAsync(ICommand command, string source) { return SendAsync(command, false, source); }
        Task ICommandProducer.DispatchAsyncAwait(ICommand command, string source) { return SendAsync(command, true, source); }
        Task<TResult?> ICommandProducer.DispatchAsyncAwait<TResult>(ICommand<TResult> command, string source) where TResult : default { return SendAsync(command, source); }
        Task IEventProducer.DispatchAsync(IEvent @event, string source) { return SendAsync(@event, source); }

        private async Task SendAsync(ICommand command, bool requireAcknowledgement, string source)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(KafkaProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(KafkaProducer)}");

            await throttle.WaitAsync();

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(KafkaCommon.TopicMaxLength);

                if (requireAcknowledgement)
                {
                    if (!listenerStarted)
                    {
                        await listenerStartedLock.WaitAsync();
                        try
                        {
                            if (!listenerStarted)
                            {
                                await KafkaCommon.EnsureTopic(host, ackTopic);
                                _ = AckListeningThread();
                                listenerStarted = true;
                            }
                        }
                        finally
                        {
                            _ = listenerStartedLock.Release();
                        }
                    }
                }

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new KafkaMessage()
                {
                    MessageData = KafkaCommon.Serialize(command),
                    MessageType = command.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
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
                        Acknowledgement? acknowledgement = null;
                        _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                        {
                            acknowledgement = ackFromCallback;
                            _ = waiter.Release();
                        });

                        var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body });
                        if (producerResult.Status != PersistenceStatus.Persisted)
                            throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                        await waiter.WaitAsync();

                        Acknowledgement.ThrowIfFailed(acknowledgement);
                    }
                    finally
                    {
                        _ = ackCallbacks.TryRemove(ackKey, out _);
                        waiter?.Dispose();
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
            finally
            {
                throttle.Release();
            }
        }

        private async Task<TResult?> SendAsync<TResult>(ICommand<TResult> command, string source)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(KafkaProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(KafkaProducer)}");

            await throttle.WaitAsync();

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(KafkaCommon.TopicMaxLength);

                if (!listenerStarted)
                {
                    await listenerStartedLock.WaitAsync();
                    try
                    {
                        if (!listenerStarted)
                        {
                            await KafkaCommon.EnsureTopic(host, ackTopic);
                            _ = AckListeningThread();
                            listenerStarted = true;
                        }
                    }
                    finally
                    {
                        _ = listenerStartedLock.Release();
                    }
                }

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new KafkaMessage()
                {
                    MessageData = KafkaCommon.Serialize(command),
                    MessageType = command.GetType(),
                    HasResult = true,
                    Claims = claims,
                    Source = source
                };

                var body = KafkaCommon.Serialize(message);
                if (symmetricConfig != null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                var ackKey = Guid.NewGuid().ToString("N");

                var headers = new Headers();
                headers.Add(new Header(KafkaCommon.AckTopicHeader, Encoding.UTF8.GetBytes(ackTopic)));
                headers.Add(new Header(KafkaCommon.AckKeyHeader, Encoding.UTF8.GetBytes(ackKey)));
                var key = KafkaCommon.MessageWithAckKey;

                var waiter = new SemaphoreSlim(0, 1);

                try
                {
                    Acknowledgement? acknowledgement = null;
                    _ = ackCallbacks.TryAdd(ackKey, (ackFromCallback) =>
                    {
                        acknowledgement = ackFromCallback;
                        _ = waiter.Release();
                    });

                    var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Headers = headers, Key = key, Value = body });
                    if (producerResult.Status != PersistenceStatus.Persisted)
                        throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");

                    await waiter.WaitAsync();

                    var result = (TResult?)Acknowledgement.GetResultOrThrowIfFailed(acknowledgement);

                    return result;
                }
                finally
                {
                    _ = ackCallbacks.TryRemove(ackKey, out _);
                    waiter?.Dispose();
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task SendAsync(IEvent @event, string source)
        {
            var eventType = @event.GetType();
            if (!topicsByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(KafkaProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(KafkaProducer)}");

            await throttle.WaitAsync();

            try
            {
                if (!String.IsNullOrWhiteSpace(environment))
                    topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic);
                else
                    topic = topic.Truncate(KafkaCommon.TopicMaxLength);

                string[][]? claims = null;
                if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                    claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                var message = new KafkaMessage()
                {
                    MessageData = KafkaCommon.Serialize(@event),
                    MessageType = @event.GetType(),
                    HasResult = false,
                    Claims = claims,
                    Source = source
                };

                var body = KafkaCommon.Serialize(message);
                if (symmetricConfig != null)
                    body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                var producerResult = await producer.ProduceAsync(topic, new Message<string, byte[]> { Key = KafkaCommon.MessageKey, Value = body });
                if (producerResult.Status != PersistenceStatus.Persisted)
                    throw new Exception($"{nameof(KafkaProducer)} failed: {producerResult.Status}");
            }
            finally
            {
                throttle.Release();
            }
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
                    try
                    {
                        for (; ; )
                        {
                            var consumerResult = consumer.Consume(canceller.Token);
                            consumer.Commit(consumerResult);

                            if (!ackCallbacks.TryRemove(consumerResult.Message.Key, out var callback))
                                continue;

                            Acknowledgement? acknowledgement = null;
                            try
                            {
                                var response = consumerResult.Message.Value;
                                if (symmetricConfig != null)
                                    response = SymmetricEncryptor.Decrypt(symmetricConfig, response);
                                acknowledgement = KafkaCommon.Deserialize<Acknowledgement>(response);
                                acknowledgement ??= new Acknowledgement("Invalid Acknowledgement");
                            }
                            catch (Exception ex)
                            {
                                acknowledgement = new Acknowledgement(ex.Message);
                            }

                            callback(acknowledgement);

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
                if (!canceller.IsCancellationRequested)
                {
                    _ = Log.ErrorAsync(ex);
                    await Task.Delay(KafkaCommon.RetryDelay);
                    goto retry;
                }
            }
            finally
            {
                listenerStarted = false;

                try
                {
                    await KafkaCommon.DeleteTopic(host, ackTopic);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                }
                canceller.Dispose();
            }
        }

        public void Dispose()
        {
            canceller.Cancel();
            producer.Dispose();
            listenerStartedLock.Dispose();
        }

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByCommandType.ContainsKey(type))
                return;
            topicsByCommandType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
        IEnumerable<Type> ICommandProducer.GetCommandTypes()
        {
            return topicsByCommandType.Keys;
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByEventType.ContainsKey(type))
                return;
            topicsByEventType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
        }
        IEnumerable<Type> IEventProducer.GetEventTypes()
        {
            return topicsByEventType.Keys;
        }
    }
}
