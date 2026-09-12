// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.RabbitMQ
{
    public sealed class RabbitMQProducer : ICommandProducer, IEventProducer, IDisposable
    {
        //RabbitMQ's direct reply-to: a reply comes straight back to the consumer on the channel that published, with no reply queue to declare
        private const string directReplyTo = "amq.rabbitmq.reply-to";

        //guards the connection and the channel, a channel isn't safe to publish on from more than one thread
        private readonly object locker = new();

        private readonly SymmetricConfig? symmetricConfig;
        private readonly string? environment;
        private readonly ConcurrentDictionary<Type, string> topicsByCommandType;
        private readonly ConcurrentDictionary<Type, string> topicsByEventType;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> throttleByTopic;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Acknowledgement>> ackCallbacks;
        //the topics this producer has declared, only used under the lock
        private readonly HashSet<string> declaredTopics;
        private readonly ConnectionFactory factory;
        private IConnection? connection = null;
        private IModel? channel = null;

        public RabbitMQProducer(string host, SymmetricConfig? symmetricConfig, string? environment)
        {
            if (String.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            this.symmetricConfig = symmetricConfig;
            this.environment = environment;
            this.topicsByCommandType = new();
            this.topicsByEventType = new();
            this.throttleByTopic = new();
            this.ackCallbacks = new();
            this.declaredTopics = new();

            this.factory = RabbitMQCommon.CreateConnectionFactory(host);
            try
            {
                //opened now so the first send doesn't wait for the channel and its reply consumer
                lock (locker)
                    _ = OpenChannel();
            }
            catch (Exception ex)
            {
                _ = Log.ErrorAsync(ex);
                throw;
            }
        }

        string ICommandProducer.MessageHost => "[Host has Secrets]";
        string IEventProducer.MessageHost => "[Host has Secrets]";

        Task ICommandProducer.DispatchAsync(ICommand command, string source, CancellationToken cancellationToken) => SendAsync(command, false, source, cancellationToken);
        Task ICommandProducer.DispatchAwaitAsync(ICommand command, string source, CancellationToken cancellationToken) => SendAsync(command, true, source, cancellationToken);
        Task<TResult> ICommandProducer.DispatchAwaitAsync<TResult>(ICommand<TResult> command, string source, CancellationToken cancellationToken) where TResult : default => SendAsync(command, source, cancellationToken);
        Task IEventProducer.DispatchAsync(IEvent @event, string source, CancellationToken cancellationToken) => SendAsync(@event, source, cancellationToken);

        private async Task SendAsync(ICommand command, bool requireAcknowledgement, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                try
                {
                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = RabbitMQCommon.Serialize(command),
                        MessageType = command.GetType(),
                        HasResult = false,
                        Claims = claims,
                        Source = source
                    };

                    var body = RabbitMQCommon.Serialize(rabbitMessage);
                    if (symmetricConfig is not null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    if (requireAcknowledgement)
                    {
                        var correlationId = Guid.NewGuid().ToString("N");

                        //completed by the reply consumer, continuations run off its thread so it goes back to receiving
                        var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                        try
                        {
                            //added before publishing, a reply that arrives first would otherwise be missed
                            _ = ackCallbacks.TryAdd(correlationId, waiter);

                            lock (locker)
                            {
                                var channel = this.channel is not null && this.channel.IsOpen ? this.channel : OpenChannel();
                                DeclareTopic(channel, topic, true);
                                var properties = channel.CreateBasicProperties();
                                properties.ReplyTo = directReplyTo;
                                properties.CorrelationId = correlationId;
                                channel.BasicPublish(topic, String.Empty, properties, body);
                            }

                            Acknowledgement acknowledgement;
                            using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                                acknowledgement = await waiter.Task;

                            Acknowledgement.ThrowIfFailed(acknowledgement);
                        }
                        finally
                        {
                            _ = ackCallbacks.TryRemove(correlationId, out _);
                        }
                    }
                    else
                    {
                        lock (locker)
                        {
                            var channel = this.channel is not null && this.channel.IsOpen ? this.channel : OpenChannel();
                            DeclareTopic(channel, topic, true);
                            channel.BasicPublish(topic, String.Empty, channel.CreateBasicProperties(), body);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, string source, CancellationToken cancellationToken)
        {
            var commandType = command.GetType();
            if (!topicsByCommandType.TryGetValue(commandType, out var topic))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{commandType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                try
                {
                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = RabbitMQCommon.Serialize(command),
                        MessageType = command.GetType(),
                        HasResult = true,
                        Claims = claims,
                        Source = source
                    };

                    var body = RabbitMQCommon.Serialize(rabbitMessage);
                    if (symmetricConfig is not null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    var correlationId = Guid.NewGuid().ToString("N");

                    //completed by the reply consumer, continuations run off its thread so it goes back to receiving
                    var waiter = new TaskCompletionSource<Acknowledgement>(TaskCreationOptions.RunContinuationsAsynchronously);

                    try
                    {
                        //added before publishing, a reply that arrives first would otherwise be missed
                        _ = ackCallbacks.TryAdd(correlationId, waiter);

                        lock (locker)
                        {
                            var channel = this.channel is not null && this.channel.IsOpen ? this.channel : OpenChannel();
                            DeclareTopic(channel, topic, true);
                            var properties = channel.CreateBasicProperties();
                            properties.ReplyTo = directReplyTo;
                            properties.CorrelationId = correlationId;
                            channel.BasicPublish(topic, String.Empty, properties, body);
                        }

                        Acknowledgement acknowledgement;
                        using (cancellationToken.Register(static (state) => ((TaskCompletionSource<Acknowledgement>)state!).TrySetCanceled(), waiter))
                            acknowledgement = await waiter.Task;

                        var result = (TResult)Acknowledgement.GetResultOrThrowIfFailed(acknowledgement)!;

                        return result;
                    }
                    finally
                    {
                        _ = ackCallbacks.TryRemove(correlationId, out _);
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        private async Task SendAsync(IEvent @event, string source, CancellationToken cancellationToken)
        {
            var eventType = @event.GetType();
            if (!topicsByEventType.TryGetValue(eventType, out var topic))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");
            if (!throttleByTopic.TryGetValue(topic, out var throttle))
                throw new Exception($"{eventType.GetNiceName()} is not registered with {nameof(RabbitMQProducer)}");

            await throttle.WaitAsync(cancellationToken);

            try
            {
                try
                {
                    string[][]? claims = null;
                    if (Thread.CurrentPrincipal is ClaimsPrincipal principal)
                        claims = principal.Claims.Select(x => new string[] { x.Type, x.Value }).ToArray();

                    var rabbitMessage = new RabbitMQMessage()
                    {
                        MessageData = RabbitMQCommon.Serialize(@event),
                        MessageType = @event.GetType(),
                        HasResult = false,
                        Claims = claims,
                        Source = source
                    };

                    var body = RabbitMQCommon.Serialize(rabbitMessage);
                    if (symmetricConfig is not null)
                        body = SymmetricEncryptor.Encrypt(symmetricConfig, body);

                    lock (locker)
                    {
                        var channel = this.channel is not null && this.channel.IsOpen ? this.channel : OpenChannel();
                        DeclareTopic(channel, topic, false);
                        channel.BasicPublish(topic, String.Empty, channel.CreateBasicProperties(), body);
                    }
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                    throw;
                }
            }
            finally
            {
                throttle.Release();
            }
        }

        //Called under the lock when there's no open channel. One channel is used for the producer's life instead of one per message, which saves
        //opening and closing it with the broker on every send, and the replies to it arrive through direct reply-to on the consumer started here.
        private IModel OpenChannel()
        {
            if (connection is null || connection.IsOpen == false)
            {
                var reconnect = connection is not null;
                this.connection?.Close();
                this.connection?.Dispose();
                this.connection = factory.CreateConnection();
                if (reconnect)
                    _ = Log.InfoAsync($"Sender Reconnected");
            }

            this.channel?.Dispose();

            var channel = connection.CreateModel();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender, e) =>
            {
                var correlationId = e.BasicProperties.CorrelationId;
                if (correlationId is null || !ackCallbacks.TryRemove(correlationId, out var waiter))
                    return;

                Acknowledgement? acknowledgement;
                try
                {
                    //the body is only valid during this callback, so it's read here
                    var acknowledgementBody = e.Body.Span;
                    if (symmetricConfig is not null)
                        acknowledgementBody = SymmetricEncryptor.Decrypt(symmetricConfig, acknowledgementBody);

                    acknowledgement = RabbitMQCommon.Deserialize<Acknowledgement>(acknowledgementBody);
                    acknowledgement ??= new Acknowledgement("Invalid Acknowledgement");
                }
                catch (Exception ex)
                {
                    acknowledgement = new Acknowledgement(ex.Message);
                }

                _ = waiter.TrySetResult(acknowledgement);
            };

            //a direct reply only reaches the channel that published the command, once it closes the replies still awaited can't arrive
            channel.ModelShutdown += (sender, e) =>
            {
                foreach (var correlationId in ackCallbacks.Keys)
                {
                    if (ackCallbacks.TryRemove(correlationId, out var waiter))
                        _ = waiter.TrySetException(new Exception($"{nameof(RabbitMQProducer)} channel closed before the acknowledgement arrived: {e.ReplyText}"));
                }
            };

            //direct reply-to requires no acknowledgements, and the consumer before publishing
            _ = channel.BasicConsume(directReplyTo, true, consumer);

            this.channel = channel;
            return channel;
        }

        //Called under the lock. The exchange, and for commands the queue, are declared the same as the consumers declare them, so a command sent
        //before any consumer has run waits in the queue instead of being dropped when the broker closes the channel for a missing exchange.
        private void DeclareTopic(IModel channel, string topic, bool isCommand)
        {
            if (declaredTopics.Contains(topic))
                return;
            if (isCommand)
            {
                channel.ExchangeDeclare(topic, ExchangeType.Direct);
                _ = channel.QueueDeclare(topic, true, false, true);
                channel.QueueBind(topic, topic, String.Empty);
            }
            else
            {
                channel.ExchangeDeclare(topic, ExchangeType.Fanout);
            }
            _ = declaredTopics.Add(topic);
        }

        public void Dispose()
        {
            lock (locker)
            {
                try
                {
                    this.channel?.Close();
                }
                catch (Exception ex)
                {
                    //the connection may already be gone
                    _ = Log.ErrorAsync(ex);
                }
                this.channel?.Dispose();
                this.channel = null;
                this.connection?.Close();
                this.connection?.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        void ICommandProducer.RegisterCommandType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByCommandType.ContainsKey(type))
                return;
            topic = BuildTopic(topic);
            topicsByCommandType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            //declared now so the first send finds it declared
            _ = Task.Run(() =>
            {
                try
                {
                    lock (locker)
                        DeclareTopic(this.channel is not null && this.channel.IsOpen ? this.channel : OpenChannel(), topic, true);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                }
            });
        }

        void IEventProducer.RegisterEventType(int maxConcurrent, string topic, Type type)
        {
            if (topicsByEventType.ContainsKey(type))
                return;
            topic = BuildTopic(topic);
            topicsByEventType.TryAdd(type, topic);
            if (throttleByTopic.ContainsKey(topic))
                return;
            var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            if (!throttleByTopic.TryAdd(topic, throttle))
                throttle.Dispose();
            //declared now so the first send finds it declared
            _ = Task.Run(() =>
            {
                try
                {
                    lock (locker)
                        DeclareTopic(this.channel is not null && this.channel.IsOpen ? this.channel : OpenChannel(), topic, false);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(ex);
                }
            });
        }

        private string BuildTopic(string topic)
        {
            if (!String.IsNullOrWhiteSpace(environment))
                return StringExtensions.Join(RabbitMQCommon.TopicMaxLength, "_", environment, topic);
            else
                return topic.Truncate(RabbitMQCommon.TopicMaxLength);
        }
    }
}
