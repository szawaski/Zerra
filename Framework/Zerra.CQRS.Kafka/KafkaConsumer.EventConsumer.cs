// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Encryption;
using Zerra.Logging;

namespace Zerra.CQRS.Kafka
{
    public sealed partial class KafkaConsumer
    {
        public sealed class EventConsumer : IDisposable
        {
            public bool IsOpen { get; private set; }

            private readonly int maxConcurrent;
            private readonly string topic;
            private readonly SymmetricConfig? symmetricConfig;
            private readonly HandleRemoteEventDispatch handlerAsync;
            private readonly CancellationTokenSource canceller;
            //well within max.poll.interval.ms, librdkafka's default is 5 minutes
            private static readonly TimeSpan busyPollInterval = TimeSpan.FromSeconds(10);
            //every event consumer gets its own group so each one receives every event, it's kept across retries and deleted when the consumer stops
            private readonly string groupId;

            public EventConsumer(int maxConcurrent, string topic, SymmetricConfig? symmetricConfig, string? environment, HandleRemoteEventDispatch handlerAsync)
            {
                if (maxConcurrent < 1) throw new ArgumentException("cannot be less than 1", nameof(maxConcurrent));

                this.maxConcurrent = maxConcurrent;

                if (!String.IsNullOrWhiteSpace(environment))
                    this.topic = StringExtensions.Join(KafkaCommon.TopicMaxLength, "_", environment, topic);
                else
                    this.topic = topic.Truncate(KafkaCommon.TopicMaxLength);
                this.symmetricConfig = symmetricConfig;
                this.handlerAsync = handlerAsync;
                this.canceller = new CancellationTokenSource();
                this.groupId = Guid.NewGuid().ToString("N");
            }

            public void Open(string host, string? userName, string? password) => Open(KafkaCommon.GetHost(host, userName, password));

            internal void Open(KafkaCommonHost commonHost)
            {
                if (IsOpen)
                    return;
                IsOpen = true;
                _ = Task.Run(() => ListeningThread(commonHost, handlerAsync));
            }

            public Task ListeningThread(string host, string? userName, string? password, HandleRemoteEventDispatch handlerAsync) => ListeningThread(KafkaCommon.GetHost(host, userName, password), handlerAsync);

            private async Task ListeningThread(KafkaCommonHost commonHost, HandleRemoteEventDispatch handlerAsync)
            {
                //the throttle is not disposed: handlers still running after the listener stops release it, and a disposed SemaphoreSlim throws ObjectDisposedException on Release
                //this isn't a leak, SemaphoreSlim.Dispose only frees the wait handle that AvailableWaitHandle creates on first use, which nothing reads,
                //the rest is managed memory with no finalizer that the GC reclaims once nothing references it
                //if AvailableWaitHandle is ever used, dispose it once every handler that could release it has finished
                var throttle = new SemaphoreSlim(maxConcurrent, maxConcurrent);

            retry:

                try
                {
                    await KafkaCommon.EnsureTopic(commonHost, topic);

                    var consumerConfig = new ConsumerConfig();
                    consumerConfig.BootstrapServers = commonHost.Host;
                    consumerConfig.GroupId = groupId;
                    consumerConfig.EnableAutoCommit = false;
                    if (commonHost.UserName is not null && commonHost.Password is not null)
                    {
                        consumerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                        consumerConfig.SaslMechanism = SaslMechanism.Plain;
                        consumerConfig.SaslUsername = commonHost.UserName;
                        consumerConfig.SaslPassword = commonHost.Password;
                    }

                    //librdkafka only reports a deleted topic as an error and keeps waiting, so it ends the consume and the retry creates the topic again
                    using var topicMissing = CancellationTokenSource.CreateLinkedTokenSource(canceller.Token);
                    using (var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).SetErrorHandler((_, error) =>
                    {
                        if (error.Code == ErrorCode.UnknownTopicOrPart || error.Code == ErrorCode.Local_UnknownPartition)
                            topicMissing.Cancel();
                    }).Build())
                    {
                        consumer.Subscribe(topic);

                        try
                        {
                            for (; ; )
                            {
                                if (!throttle.Wait(0))
                                {
                                    //librdkafka drops a consumer from the group when it isn't polled within max.poll.interval.ms,
                                    //so while every handler is busy the partitions are paused and still polled
                                    consumer.Pause(consumer.Assignment);
                                    while (!await throttle.WaitAsync(busyPollInterval, topicMissing.Token))
                                    {
                                        var held = consumer.Consume(TimeSpan.Zero);
                                        if (held is not null)
                                        {
                                            //from a partition assigned since the pause, it's read again once resumed
                                            consumer.Seek(held.TopicPartitionOffset);
                                            consumer.Pause(consumer.Assignment);
                                        }
                                    }
                                    consumer.Resume(consumer.Assignment);
                                }

                                ConsumeResult<string, byte[]> consumerResult;
                                try
                                {
                                    consumerResult = consumer.Consume(topicMissing.Token);
                                    consumer.Commit(consumerResult);
                                }
                                catch
                                {
                                    //the retry keeps this throttle, give back the permit taken for the message that wasn't received
                                    //an uncommitted message is consumed again from the last committed offset after reconnecting
                                    _ = throttle.Release();
                                    throw;
                                }

                                _ = Task.Run(() => HandleMessage(throttle, consumerResult, handlerAsync));

                                if (canceller.IsCancellationRequested)
                                    break;
                            }
                        }
                        finally
                        {
                            //Close leaves the group right away, Unsubscribe and Dispose leave its member until the session times out
                            consumer.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //closing cancels the consume, that isn't an error
                    if (!canceller.IsCancellationRequested)
                    {
                        _ = Log.ErrorAsync(topic, ex);
                        await KafkaCommon.ForgetTopic(commonHost, topic);
                        await Task.Delay(KafkaCommon.RetryDelay);
                        goto retry;
                    }
                }

                //only reached once the consumer is stopping and has left the group
                try
                {
                    await KafkaCommon.DeleteConsumerGroup(commonHost, groupId);
                }
                catch (Exception ex)
                {
                    _ = Log.ErrorAsync(topic, ex);
                }
            }

            private async Task HandleMessage(SemaphoreSlim throttle, ConsumeResult<string, byte[]> consumerResult, HandleRemoteEventDispatch handlerAsync)
            {
                var inHandlerContext = false;
                try
                {
                    if (consumerResult.Message.Key == KafkaCommon.MessageKey)
                    {
                        var body = consumerResult.Message.Value;
                        if (symmetricConfig is not null)
                            body = SymmetricEncryptor.Decrypt(symmetricConfig, body);

                        var message = KafkaCommon.Deserialize<KafkaMessage>(body);

                        if (message is null || message.MessageType is null || message.MessageData is null || message.Source is null)
                            throw new Exception("Invalid Message");

                        var @event = KafkaCommon.Deserialize(message.MessageData, message.MessageType) as IEvent;
                        if (@event is null)
                            throw new Exception("Invalid Message");

                        if (message.Claims is not null)
                        {
                            var claimsIdentity = new ClaimsIdentity(message.Claims.Select(x => new Claim(x[0], x[1])), "CQRS");
                            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
                        }

                        inHandlerContext = true;
                        await handlerAsync(@event, message.Source, false);
                        inHandlerContext = false;
                    }
                    else
                    {
                        _ = Log.ErrorAsync($"{nameof(KafkaConsumer)} unrecognized message key {consumerResult.Message.Key}");
                    }
                }
                catch (Exception ex)
                {
                    //the bus logs handler errors, anything before the handler such as a message that can't be read is logged here
                    if (!inHandlerContext)
                        _ = Log.ErrorAsync(topic, ex);
                }
                finally
                {
                    throttle.Release();
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
