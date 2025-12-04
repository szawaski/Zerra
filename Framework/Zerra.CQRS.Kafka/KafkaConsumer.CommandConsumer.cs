// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System.Security.Claims;
using System.Text;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.CQRS.Network;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        private sealed class CommandConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly CommandCounter commandCounter;
            private readonly string topic;
            private readonly string clientID;
            private readonly Zerra.Serialization.ISerializer serializer;
            private readonly IEncryptor? encryptor;
            private readonly ILog? log;
            private readonly HandleRemoteCommandDispatch handlerAsync;
            private readonly HandleRemoteCommandDispatch handlerAwaitAsync;
            private readonly HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync;
            private readonly CancellationTokenSource canceller;

            public CommandConsumer(int maxConcurrent, CommandCounter commandCounter, string topic, Zerra.Serialization.ISerializer serializer, IEncryptor? encryptor, ILog? log, string? environment, HandleRemoteCommandDispatch handlerAsync, HandleRemoteCommandDispatch handlerAwaitAsync, HandleRemoteCommandWithResultDispatch handlerWithResultAwaitAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = commandCounter.ReceiveCountBeforeExit.HasValue ? Math.Min(commandCounter.ReceiveCountBeforeExit.Value, maxConcurrent) : maxConcurrent;
                this.commandCounter = commandCounter;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength);
                this.clientID = Environment.MachineName;
                this.serializer = serializer;
                this.encryptor = encryptor;
                this.log = log;
                this.handlerAsync = handlerAsync;
                this.handlerAwaitAsync = handlerAwaitAsync;
                this.handlerWithResultAwaitAsync = handlerWithResultAwaitAsync;
                this.canceller = new CancellationTokenSource();
            }

            public void Open(string host, string? userName, string? password)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(host, userName, password));
            }

            private async Task ListeningThread(string host, string? userName, string? password)
            {
            retry:

                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

                try
                {
                    await KafkaCommon.EnsureTopic(host, userName, password, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = host;
                    consumerConfig.GroupId = topic;
                    consumerConfig.EnableAutoCommit = false;
                    if (userName is not null && password is not null)
                    {
                        consumerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                        consumerConfig.SaslMechanism = SaslMechanism.Plain;
                        consumerConfig.SaslUsername = userName;
                        consumerConfig.SaslPassword = password;
                    }

                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build())
                    {
                        consumer.Subscribe(topic);
                        try
                        {
                            for (; ; )
                            {
                                await throttle.WaitAsync(canceller.Token);

                                if (!commandCounter.BeginReceive())
                                    continue; //don't receive anymore, externally will be shutdown, fill throttle

                                var consumerResult = consumer.Consume(canceller.Token);
                                consumer.Commit(consumerResult);

                                _ = Task.Run(() => HandleMessage(throttle, host, consumerResult));

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
                    log?.Error(topic, ex);
                    if (!canceller.IsCancellationRequested)
                    {
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }
                finally
                {
                    throttle.Dispose();
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, string host, ConsumeResult<string, byte[]> consumerResult)
            {
                object? result = null;
                Exception? error = null;
                var awaitResponse = consumerResult.Message.Key == KafkaCommon.MessageWithAckKey;

                var inHandlerContext = false;
                try
                {
                    if (consumerResult.Message.Key == KafkaCommon.MessageKey || consumerResult.Message.Key == KafkaCommon.MessageWithAckKey)
                    {
                        var body = consumerResult.Message.Value;
                        if (encryptor is not null)
                            body = encryptor.Decrypt(body);

                        var message = serializer.Deserialize<KafkaMessage>(body);
                        if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                            throw new Exception("Invalid Message");

                        var command = serializer.Deserialize(message.MessageData, message.MessageType) as ICommand;
                        if (command is null)
                            throw new Exception("Invalid Message");

                        if (message.Claims is not null)
                        {
                            var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }

                        inHandlerContext = true;
                        if (message.HasResult)
                            result = await handlerWithResultAwaitAsync(command, message.Source, false, canceller.Token);
                        else if (awaitResponse)
                            await handlerAwaitAsync(command, message.Source, false, canceller.Token);
                        else
                            await handlerAsync(command, message.Source, false, default);
                        inHandlerContext = false;
                    }
                    else
                    {
                        log?.Error($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    if (!inHandlerContext)
                        log?.Error(topic, ex);
                }
                finally
                {
                    if (!awaitResponse)
                        commandCounter.CompleteReceive(throttle);
                }

                if (!awaitResponse)
                    return;

                try
                {
                    var ackTopic = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckTopicHeader));
                    var ackKey = Encoding.UTF8.GetString(consumerResult.Message.Headers.GetLastBytes(KafkaCommon.AckKeyHeader));

                    var acknowledgement = new Acknowledgement(result, error);
                    var body = serializer.SerializeBytes(acknowledgement);
                    if (encryptor is not null)
                        body = encryptor.Encrypt(body);

                    var producerConfig = new ProducerConfig();
                    producerConfig.BootstrapServers = host;
                    producerConfig.ClientId = clientID;
                    using (var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build())
                    {
                        _ = await producer.ProduceAsync(ackTopic, new Message<string, byte[]>()
                        {
                            Key = ackKey!,
                            Value = body
                        });
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(ex);
                }
                finally
                {
                    commandCounter.CompleteReceive(throttle);
                }
            }

            public void Dispose()
            {
                canceller.Cancel();
                canceller.Dispose();
            }
        }
    }
}
