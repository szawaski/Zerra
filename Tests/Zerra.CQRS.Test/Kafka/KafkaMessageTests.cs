// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using System.Collections.Concurrent;
using Xunit;
using Zerra.CQRS;
using Zerra.Compression;
using Zerra.CQRS.Kafka;
using Zerra.Encryption;
using Zerra.Reflection;
using Zerra.Serialization;

namespace Zerra.CQRS.Test.Kafka
{
    public class KafkaMessageTests
    {
        private const string host = "localhost:9092";

        [Fact]
        public async Task TestConnection()
        {
            Assert.True(await KafkaConnectionTest.TestAsync(host, null, null));
            //nothing listens on port 1
            Assert.False(await KafkaConnectionTest.TestAsync("localhost:1", null, null, TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public async Task TestConnectionTls()
        {
            Assert.True(await KafkaConnectionTest.TestAsync(host, null, null, TimeSpan.FromSeconds(5), useTls: false));
            Assert.False(await KafkaConnectionTest.TestAsync(host, null, null, TimeSpan.FromSeconds(5), useTls: true));
            Assert.False(await KafkaConnectionTest.TestAsync(host, "user", "password", TimeSpan.FromSeconds(5), useTls: true));
        }

        [Fact]
        public void TestHostPerTls()
        {
            var plain = KafkaCommon.GetHost(host, null, null);
            var tls = KafkaCommon.GetHost(host, null, null, true);

            Assert.False(plain.UseTls);
            Assert.True(tls.UseTls);
            Assert.NotSame(plain, tls);
            Assert.Same(plain, KafkaCommon.GetHost(host, null, null, false));
            Assert.Same(tls, KafkaCommon.GetHost(host, null, null, true));
        }

        [Fact]
        public async Task TestTlsProducerAndConsumerUseTlsHost()
        {
            var serializer = new ZerraByteSerializer();
            await using var producer = new KafkaProducer(host, serializer, null, null, null, null, null, null, useTls: true);
            await using var consumer = new KafkaConsumer(host, serializer, null, null, null, null, null, null, useTls: true);

            Assert.Same(KafkaCommon.GetHost(host, null, null, true), CommonHost(producer));
            Assert.Same(KafkaCommon.GetHost(host, null, null, true), CommonHost(consumer));
        }

        [Theory(Timeout = 300000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestSequence(bool resilientCommands)
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var compressor = new ZerraCompressor(CompressionAlgorithmType.Brotli);
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, encryptor, compressor, log, null, resilientCommands: resilientCommands))
                using (var producer = new KafkaProducer(host, serializer, encryptor, compressor, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestSequence(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);

                    //the acknowledgement topic is assigned, joining a group would wait out the broker's initial rebalance delay on the first command
                    Assert.False(await ConsumerGroupExists(ackTopic));
                }
            }
            finally
            {
                await Cleanup(commandTopic, eventTopic, ackTopic);
            }
        }

        [Theory(Timeout = 300000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestFinishesProcessingOnClose(bool disposeAsync)
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, encryptor, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, encryptor, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestFinishesProcessingOnClose(producer, producer, consumer, consumer, commandTopic, eventTopic, disposeAsync, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await Cleanup(commandTopic, eventTopic, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestEventConsumerModePerService()
        {
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var log = new TestLogger();

            try
            {
                //a consumer per connection, standing in for the replicas of two services
                using (var serviceAReplica1 = new KafkaConsumer(host, serializer, encryptor, null, log, null, null, null))
                using (var serviceAReplica2 = new KafkaConsumer(host, serializer, encryptor, null, log, null, null, null))
                using (var serviceB = new KafkaConsumer(host, serializer, encryptor, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, encryptor, null, log, null, null, null))
                {
                    await MessageTest.TestEventConsumerModePerService(producer, serviceAReplica1, serviceAReplica2, serviceB, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, eventTopic);

                //a PerService group is shared by the replicas, so the consumers leave it behind
                await DeleteConsumerGroup($"{eventTopic}_{MessageTest.ServiceAName}");
                await DeleteConsumerGroup($"{eventTopic}_{MessageTest.ServiceBName}");
            }
        }

        [Theory(Timeout = 300000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestSharedConsumer(bool eventFirst)
        {
            var commandTopicA = MessageTest.NewTopic("CommandA");
            var commandTopicB = MessageTest.NewTopic("CommandB");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestSharedConsumer(producer, producer, consumer, commandTopicA, commandTopicB, eventTopic, eventFirst, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await Cleanup(commandTopicA, eventTopic, ackTopic);
                await KafkaCommon.DeleteTopic(host, null, null, commandTopicB);
                await DeleteConsumerGroup(commandTopicB);
            }
        }

        //librdkafka drops a consumer that isn't polled within max.poll.interval.ms, set low here so the handler can stay busy past it quickly
        [Theory(Timeout = 300000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestBusyHandlersStayInGroup(bool events)
        {
            var topic = MessageTest.NewTopic(events ? "Event" : "Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;
            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestEvent));

            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var received = new ConcurrentQueue<Guid>();
            async Task Handle(Guid id)
            {
                received.Enqueue(id);
                await release.Task;
            }

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null) { MaxPollIntervalMs = 7000 })
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    Func<Guid, Task> send;
                    if (events)
                    {
                        ((IEventConsumer)consumer).Setup("ZerraTestService", (@event, _) => Handle(((TestEvent)@event).ID));
                        ((IEventConsumer)consumer).RegisterEventType(1, topic, typeof(TestEvent), EventConsumerMode.PerReplica);
                        ((IEventProducer)producer).RegisterEventType(1, topic, typeof(TestEvent));
                        ((IEventConsumer)consumer).Open();
                        send = (id) => ((IEventProducer)producer).DispatchAsync(new TestEvent() { ID = id }, "Test", TestContext.Current.CancellationToken);
                    }
                    else
                    {
                        ((ICommandConsumer)consumer).Setup(null, (command, _, _) => Handle(((TestCommand)command).ID), (command, _, _) => Handle(((TestCommand)command).ID), (_, _, _) => throw new NotSupportedException());
                        ((ICommandConsumer)consumer).RegisterCommandType(1, topic, typeof(TestCommand));
                        ((ICommandProducer)producer).RegisterCommandType(1, topic, typeof(TestCommand));
                        ((ICommandConsumer)consumer).Open();
                        send = (id) => ((ICommandProducer)producer).DispatchAsync(new TestCommand() { ID = id }, "Test", TestContext.Current.CancellationToken);
                    }

                    //an event sent before the new group has its partition isn't read, so it's sent again until one is handled
                    for (var attempt = 1; received.IsEmpty; attempt++)
                    {
                        if (attempt == 90)
                            throw new TimeoutException("Nothing was received");
                        await send(Guid.NewGuid());
                        await Task.Delay(1000, TestContext.Current.CancellationToken);
                    }

                    //the one handler stays busy past max.poll.interval.ms, errors before this are the new topic still being created
                    var errorsBefore = log.Errors;
                    await Task.Delay(TimeSpan.FromSeconds(12), TestContext.Current.CancellationToken);
                    release.SetResult();

                    var last = Guid.NewGuid();
                    await send(last);
                    for (var attempt = 1; !received.Contains(last); attempt++)
                    {
                        if (attempt == 300)
                            throw new TimeoutException("The command after the busy handler was not received");
                        await Task.Delay(100, TestContext.Current.CancellationToken);
                    }
                    Assert.Equal(errorsBefore, log.Errors);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, topic);
                if (!events)
                    await DeleteConsumerGroup(topic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 600000)]
        public async Task TestSustainedLoad()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var replica1 = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var replica2 = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestSustainedLoad(producer, producer, replica1, replica2, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
                Assert.Equal(0, log.Errors);
            }
            finally
            {
                await Cleanup(commandTopic, eventTopic, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestReceiveLimitHandsOff()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var replica1 = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var replica2 = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestReceiveLimitHandsOff(producer, replica1, replica2, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestResilientCommandDeliveredAgain()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                //replica1 waits for its stuck command before giving up the partition, so it stops polling and the group drops it after max.poll.interval.ms,
                //which leaves the partition uncommitted as a killed process would
                using (var replica1 = new KafkaConsumer(host, serializer, null, null, log, null, resilientCommands: true) { MaxPollIntervalMs = 7000 })
                using (var replica2 = new KafkaConsumer(host, serializer, null, null, log, null, resilientCommands: true) { MaxPollIntervalMs = 7000 })
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestResilientCommandDeliveredAgain(producer, replica1, replica2, commandTopic, () => Task.CompletedTask, TimeSpan.FromSeconds(90), TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Theory(Timeout = 120000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestAckListenerStartsOnRegister(bool disposeAsync)
        {
            var commandTopic = MessageTest.NewTopic("Command");
            string? ackTopic = null;

            try
            {
                var producer = new KafkaProducer(host, new ZerraByteSerializer(), null, null, new TestLogger(), null, null, null);
                ackTopic = AckTopic(producer);
                ((ICommandProducer)producer).RegisterCommandType(1, commandTopic, typeof(TestCommand));

                //registering starts the listener, the first command doesn't wait for the topic
                await WaitUntilTopicExists(ackTopic, TestContext.Current.CancellationToken);

                if (disposeAsync)
                    await producer.DisposeAsync();
                else
                    producer.Dispose();

                using (var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build())
                    Assert.DoesNotContain(admin.GetMetadata(TimeSpan.FromSeconds(10)).Topics, x => x.Topic == ackTopic && x.Error.Code == ErrorCode.NoError);
            }
            finally
            {
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 120000)]
        public async Task TestRegistrationRules()
        {
            var commandTopic = MessageTest.NewTopic("Rules");
            var eventTopic = MessageTest.NewTopic("RulesEvent");
            var serializer = new ZerraByteSerializer();
            string? ackTopic = null;
            try
            {
                await using var consumer = new KafkaConsumer(host, serializer, null, null, new TestLogger(), null, null, null);
                await using var producer = new KafkaProducer(host, serializer, null, null, new TestLogger(), null, null, null);
                ackTopic = AckTopic(producer);
                await MessageEdgeTest.TestRegistrationRules(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
            }
            finally
            {
                await Cleanup(commandTopic, eventTopic, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestEnvironmentRoundTrip()
        {
            const string environment = "ZerraEdge";
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;
            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, environment, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, environment, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageEdgeTest.TestRoundTrip(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await Cleanup($"{environment}_{commandTopic}", $"{environment}_{eventTopic}", ackTopic);
            }
            Assert.Equal(0, log.Errors);
        }

        [Fact(Timeout = 300000)]
        public async Task TestReceiveLimitAwaited()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;
            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageEdgeTest.TestReceiveLimitAwaited(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestMalformedMessages()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;
            using var raw = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = host }).Build();
            async Task Send(string topic, string key, byte[] body) => _ = await raw.ProduceAsync(topic, new Message<string, byte[]> { Key = key, Value = body });
            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageEdgeTest.TestMalformedMessages(producer, producer, consumer, consumer, commandTopic, eventTopic, serializer, log,
                        (type, data, source) => serializer.SerializeBytes(new KafkaMessage { MessageType = type, MessageData = data, Source = source }),
                        body => Send(commandTopic, KafkaCommon.MessageKey, body),
                        body => Send(eventTopic, KafkaCommon.MessageKey, body),
                        async () =>
                        {
                            //a key that isn't a message
                            await Send(commandTopic, "Unknown", [1]);
                            await Send(eventTopic, "Unknown", [1]);
                            return 2;
                        },
                        TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await Cleanup(commandTopic, eventTopic, ackTopic);
            }
        }

        private static ICollection<string> PendingAckKeys(KafkaProducer producer) => ((System.Collections.IDictionary)typeof(KafkaProducer).GetField("ackCallbacks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producer)!).Keys.Cast<string>().ToArray();

        [Fact(Timeout = 300000)]
        public async Task TestInvalidAcknowledgement()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            string? ackTopic = null;
            using var raw = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = host }).Build();
            try
            {
                using var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null);
                using var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null);
                try
                {
                    ackTopic = AckTopic(producer);
                    TypeFinder.Register(typeof(TestCommand));
                    async Task Handle(ICommand command, string commandSource, CancellationToken cancellationToken)
                    {
                        started.TrySetResult();
                        await release.Task;
                    }
                    ((ICommandConsumer)consumer).Setup(null, Handle, Handle, (_, _, _) => Task.FromResult<object?>(null));
                    ((ICommandConsumer)consumer).RegisterCommandType(4, commandTopic, typeof(TestCommand));
                    ((ICommandProducer)producer).RegisterCommandType(4, commandTopic, typeof(TestCommand));
                    ((ICommandConsumer)consumer).Open();

                    var awaiting = ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand { ID = Guid.NewGuid() }, "test", TestContext.Current.CancellationToken);
                    await started.Task.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

                    //an acknowledgement that can't be read fails the call instead of leaving it waiting
                    var key = Assert.Single(PendingAckKeys(producer));
                    _ = await raw.ProduceAsync(ackTopic, new Message<string, byte[]> { Key = key, Value = [1, 2, 3] }, TestContext.Current.CancellationToken);
                    _ = await Assert.ThrowsAnyAsync<Exception>(() => awaiting.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken));

                    //an acknowledgement nobody is waiting for is ignored
                    _ = await raw.ProduceAsync(ackTopic, new Message<string, byte[]> { Key = "unknown", Value = [1] }, TestContext.Current.CancellationToken);
                }
                finally
                {
                    //disposing the consumer waits for the handler
                    release.TrySetResult();
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestAckTopicDeleted()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;
            try
            {
                using var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null);
                using var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null);
                ackTopic = AckTopic(producer);
                TypeFinder.Register(typeof(TestCommand));
                ((ICommandConsumer)consumer).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
                ((ICommandConsumer)consumer).RegisterCommandType(4, commandTopic, typeof(TestCommand));
                ((ICommandProducer)producer).RegisterCommandType(4, commandTopic, typeof(TestCommand));
                ((ICommandConsumer)consumer).Open();

                await ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand { ID = Guid.NewGuid() }, "test", TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

                //deleted outside Zerra while the producer listens on it, awaited commands still get their acknowledgements
                await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
                await ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand { ID = Guid.NewGuid() }, "test", TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(120), TestContext.Current.CancellationToken);
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 120000)]
        public async Task TestLongNamesTruncated()
        {
            //nothing is created until a consumer opens or a producer sends
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            var environment = new string('e', 300);
            await using (var producer = new KafkaProducer(host, serializer, null, null, log, environment, null, null))
            await using (var consumer = new KafkaConsumer(host, serializer, null, null, log, environment, null, null))
            {
                ((ICommandProducer)producer).RegisterCommandType(1, "topic", typeof(TestCommand));
                ((IEventProducer)producer).RegisterEventType(1, "topic", typeof(TestEvent));
                ((ICommandConsumer)consumer).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
                ((IEventConsumer)consumer).Setup("EdgeService", (_, _) => Task.CompletedTask);
                ((ICommandConsumer)consumer).RegisterCommandType(1, "topic", typeof(TestCommand));
                ((IEventConsumer)consumer).RegisterEventType(1, "topic", typeof(TestEvent), EventConsumerMode.PerService);
            }
            //the client ID, the producer's command and event topics, and the consumer's
            Assert.True(log.Warnings >= 5, $"{log.Warnings} warnings");
        }

        [Fact(Timeout = 120000)]
        public async Task TestCredentialsConfigure()
        {
            //a user name and password set up SASL, plain or over TLS, nothing connects until a message is sent
            var serializer = new ZerraByteSerializer();
            await using (var producer = new KafkaProducer(host, serializer, null, null, null, null, "user", "password"))
            await using (var tlsProducer = new KafkaProducer(host, serializer, null, null, null, null, "user", "password", useTls: true))
            {
                Assert.Equal("[Host has Secrets]", ((ICommandProducer)producer).MessageHost);
            }
            var commonHost = KafkaCommon.GetHost(host, "user", "password");
            Assert.Equal("user", commonHost.UserName);
            Assert.Same(commonHost, KafkaCommon.GetHost(host, "user", "password"));
        }

        private static async Task Cleanup(string commandTopic, string eventTopic, string? ackTopic)
        {
            await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
            await KafkaCommon.DeleteTopic(host, null, null, eventTopic);

            //command consumers share a group named after the topic, so it outlives them, event consumer groups are deleted by the consumers themselves
            await DeleteConsumerGroup(commandTopic);

            //the producer deletes its acknowledgement topic when it's disposed, this catches it if that didn't finish. The topic is assigned, not subscribed, so there's no group
            if (ackTopic is not null)
                await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
        }

        [Fact(Timeout = 300000)]
        public async Task TestCommandSentBeforeConsumer()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestCommandSentBeforeConsumer(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestHandlerErrorNotReceivedAgain()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    await MessageTest.TestHandlerErrorNotReceivedAgain(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await KafkaCommon.DeleteTopic(host, null, null, eventTopic);
                await DeleteConsumerGroup(commandTopic);
                await DeleteConsumerGroup($"{eventTopic}_{MessageTest.ServiceAName}");
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestConsumesAgainAfterTopicDeleted()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    //deleted with the broker's own admin client, not KafkaCommon, so the cached topic list still has it
                    await MessageTest.TestConsumesAgainAfterTopicDeleted(producer, consumer, commandTopic, async () =>
                    {
                        using var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
                        await admin.DeleteTopicsAsync([commandTopic]);
                    }, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestAckProducerReplacedAfterManyTopics()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            var ackTopics = new List<string>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
            Zerra.Reflection.TypeFinder.Register(typeof(TestCommand));

            //each producer's acknowledgements go to its own topic
            async Task SendFromNewProducer()
            {
                using var producer = new KafkaProducer(host, serializer, null, null, log, null, null, null);
                ackTopics.Add(AckTopic(producer));
                ((ICommandProducer)producer).RegisterCommandType(10, commandTopic, typeof(TestCommand));
                await ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand() { ID = Guid.NewGuid() }, "Zerra.CQRS.Test", TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(90), TestContext.Current.CancellationToken);
            }

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                {
                    var commandConsumer = (ICommandConsumer)consumer;
                    commandConsumer.Setup(null, (command, source, cancellationToken) => Task.CompletedTask, (command, source, cancellationToken) => Task.CompletedTask, (command, source, cancellationToken) => Task.FromResult<object?>(null));
                    commandConsumer.RegisterCommandType(10, commandTopic, typeof(TestCommand));
                    commandConsumer.Open();

                    await SendFromNewProducer();

                    var exchange = ((System.Collections.IDictionary)typeof(KafkaConsumer).GetField("commandExchanges", flags)!.GetValue(consumer)!)[commandTopic]!;
                    var ackProducerField = exchange.GetType().GetField("ackProducer", flags)!;
                    var first = ackProducerField.GetValue(exchange)!;
                    first.GetType().GetField("TopicCount", flags)!.SetValue(first, 999);

                    //the thousandth topic replaces the producer, and the old one is disposed once its last reply is sent
                    await SendFromNewProducer();
                    var second = ackProducerField.GetValue(exchange)!;
                    Assert.NotSame(first, second);
                    var disposedField = first.GetType().GetField("disposed", flags)!;
                    for (var attempt = 1; (int)disposedField.GetValue(first)! == 0; attempt++)
                    {
                        Assert.True(attempt < 50, "The replaced producer was not disposed");
                        await Task.Delay(100, TestContext.Current.CancellationToken);
                    }

                    await SendFromNewProducer();
                    Assert.Same(second, ackProducerField.GetValue(exchange));
                }
            }
            finally
            {
                await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
                await DeleteConsumerGroup(commandTopic);
                foreach (var ackTopic in ackTopics)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
        }

        [Fact(Timeout = 120000)]
        public async Task TestReplicasEnsureAtOnce()
        {
            var topic = MessageTest.NewTopic("Command");

            //a host each, as replicas in separate processes have, so none of them know the others created it
            var replicas = Enumerable.Range(0, 8).Select(_ => new KafkaCommonHost(host, null, null, false)).ToArray();
            try
            {
                await Task.WhenAll(replicas.Select(x => KafkaCommon.EnsureTopic(x, topic).AsTask()));
                await WaitUntilTopicExists(topic, TestContext.Current.CancellationToken);
            }
            finally
            {
                foreach (var replica in replicas)
                    replica.Client.Dispose();
                await KafkaCommon.DeleteTopic(host, null, null, topic);
            }
        }

        private static KafkaCommonHost CommonHost(object producerOrConsumer) => (KafkaCommonHost)producerOrConsumer.GetType().GetField("commonHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producerOrConsumer)!;

        private static string AckTopic(KafkaProducer producer) =>(string)typeof(KafkaProducer).GetField("ackTopic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producer)!;

        private static async Task<bool> ConsumerGroupExists(string group)
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
            var groups = await admin.ListConsumerGroupsAsync();
            return groups.Valid.Any(x => x.GroupId == group);
        }

        //lists every topic, asking for one by name could auto-create it
        private static async Task WaitUntilTopicExists(string topic, CancellationToken cancellationToken)
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
            for (var attempt = 1; ; attempt++)
            {
                if (admin.GetMetadata(TimeSpan.FromSeconds(10)).Topics.Any(x => x.Topic == topic && x.Error.Code == ErrorCode.NoError))
                    return;
                if (attempt == 60)
                    throw new TimeoutException($"Topic {topic} was not created");
                await Task.Delay(500, cancellationToken);
            }
        }

        //consumers leave their group on a background thread after they're closed, and a group can't be deleted while it still has members
        private static async Task DeleteConsumerGroup(string group)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await KafkaCommon.DeleteConsumerGroup(host, null, null, group);
                    return;
                }
                catch when (attempt < 20)
                {
                    await Task.Delay(500);
                }
            }
        }
    }
}
