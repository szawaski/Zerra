// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.Compression;
using Zerra.CQRS.AzureServiceBus;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.CQRS.Test.AzureServiceBus
{
    public class AzureServiceBusMessageTests
    {
        //the Service Bus emulator from Demo/Infrastructure, its AMQP port is moved to 5673 since RabbitMQ owns 5672
        private const string host = "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        [Fact]
        public async Task TestConnection()
        {
            Assert.True(await AzureServiceBusConnectionTest.TestAsync(host, null));
            //not the emulator, so its management port isn't substituted, and nothing listens on port 1
            Assert.False(await AzureServiceBusConnectionTest.TestAsync("Endpoint=sb://localhost:1;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;", TimeSpan.FromSeconds(2)));
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

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, encryptor, compressor, log, null, resilientCommands))
                await using (var producer = new AzureServiceBusProducer(host, serializer, encryptor, compressor, log, null))
                {
                    await MessageTest.TestSequence(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                //commands use a queue per topic and events a topic, the event subscriptions go with the topic
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
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

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestSharedConsumer(producer, producer, consumer, commandTopicA, commandTopicB, eventTopic, eventFirst, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopicA);
                await AzureServiceBusCommon.DeleteQueue(host, commandTopicB);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
            }
        }

        [Fact(Timeout = 600000)]
        public async Task TestSustainedLoad()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var replica1 = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var replica2 = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestSustainedLoad(producer, producer, replica1, replica2, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
                Assert.Equal(0, log.Errors);
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestReceiveLimitHandsOff()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var replica1 = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var replica2 = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestReceiveLimitHandsOff(producer, replica1, replica2, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestResilientCommandDeliveredAgain()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var replica1 = new AzureServiceBusConsumer(host, serializer, null, null, log, null, true))
                await using (var replica2 = new AzureServiceBusConsumer(host, serializer, null, null, log, null, true))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    //closing the connection is what the broker sees when the process is killed, the lock then expires and the command is delivered again
                    var client = (global::Azure.Messaging.ServiceBus.ServiceBusClient)typeof(AzureServiceBusConsumer).GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(replica1)!;
                    await MessageTest.TestResilientCommandDeliveredAgain(producer, replica1, replica2, commandTopic, async () => await client.DisposeAsync(), TimeSpan.FromSeconds(150), TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestCommandSentBeforeConsumer()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestCommandSentBeforeConsumer(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestHandlerErrorNotReceivedAgain()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestHandlerErrorNotReceivedAgain(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestConsumesAgainAfterQueueDeleted()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    //deleted with the administration client directly, not AzureServiceBusCommon, so the cached queue list still has it
                    await MessageTest.TestConsumesAgainAfterTopicDeleted(producer, consumer, commandTopic, async () =>
                    {
                        _ = await AzureServiceBusCommon.CreateAdministrationClient(host).DeleteQueueAsync(commandTopic);
                    }, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Theory(Timeout = 120000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestAckListenerStartsOnRegister(bool disposeAsync)
        {
            var commandTopic = MessageTest.NewTopic("Command");
            string? ackQueue = null;

            try
            {
                var producer = new AzureServiceBusProducer(host, new ZerraByteSerializer(), null, null, new TestLogger(), null);
                ackQueue = (string)typeof(AzureServiceBusProducer).GetField("ackQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producer)!;
                ((ICommandProducer)producer).RegisterCommandType(1, commandTopic, typeof(TestCommand));

                //registering starts the listener, the first command doesn't wait for the queue
                var admin = AzureServiceBusCommon.CreateAdministrationClient(host);
                for (var attempt = 1; !(await admin.QueueExistsAsync(ackQueue, TestContext.Current.CancellationToken)).Value; attempt++)
                {
                    if (attempt == 60)
                        throw new TimeoutException($"Queue {ackQueue} was not created");
                    await Task.Delay(500, TestContext.Current.CancellationToken);
                }

                if (disposeAsync)
                    await producer.DisposeAsync();
                else
                    producer.Dispose();

                Assert.False((await admin.QueueExistsAsync(ackQueue, TestContext.Current.CancellationToken)).Value);
            }
            finally
            {
                if (ackQueue is not null)
                    await AzureServiceBusCommon.DeleteQueue(host, ackQueue);
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

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, encryptor, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, encryptor, null, log, null))
                {
                    await MessageTest.TestFinishesProcessingOnClose(producer, producer, consumer, consumer, commandTopic, eventTopic, disposeAsync, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                //commands use a queue per topic and events a topic, the event subscriptions go with the topic
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
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
                //a consumer per client, standing in for the replicas of two services
                await using (var serviceAReplica1 = new AzureServiceBusConsumer(host, serializer, encryptor, null, log, null))
                await using (var serviceAReplica2 = new AzureServiceBusConsumer(host, serializer, encryptor, null, log, null))
                await using (var serviceB = new AzureServiceBusConsumer(host, serializer, encryptor, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, encryptor, null, log, null))
                {
                    await MessageTest.TestEventConsumerModePerService(producer, serviceAReplica1, serviceAReplica2, serviceB, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                //a PerService subscription is shared by the replicas so it's left behind, deleting the topic takes it with it
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
            }
        }

        [Fact(Timeout = 120000)]
        public async Task TestReplicasEnsureAtOnce()
        {
            var queue = MessageTest.NewTopic("Command");
            var topic = MessageTest.NewTopic("Event");
            var updatedQueue = MessageTest.NewTopic("Command");

            try
            {
                //a namespace each, as replicas in separate processes have, so none of them know the others created it
                AzureServiceBusCommonNamespace[] Replicas() => Enumerable.Range(0, 8).Select(_ => new AzureServiceBusCommonNamespace(host)).ToArray();

                var replicas = Replicas();
                await Task.WhenAll(replicas.Select(x => AzureServiceBusCommon.EnsureQueue(x, queue, false).AsTask()));
                await Task.WhenAll(replicas.Select(x => AzureServiceBusCommon.EnsureTopic(x, topic, false).AsTask()));
                await Task.WhenAll(replicas.Select(x => AzureServiceBusCommon.EnsureSubscription(x, topic, "Shared", false).AsTask()));

                //settings that don't match have every replica update them at once
                _ = await AzureServiceBusCommon.CreateAdministrationClient(host).CreateQueueAsync(new Azure.Messaging.ServiceBus.Administration.CreateQueueOptions(updatedQueue) { AutoDeleteOnIdle = TimeSpan.FromHours(1) }, TestContext.Current.CancellationToken);
                replicas = Replicas();
                await Task.WhenAll(replicas.Select(x => AzureServiceBusCommon.EnsureQueue(x, updatedQueue, false).AsTask()));
                var updated = await AzureServiceBusCommon.CreateAdministrationClient(host).GetQueueAsync(updatedQueue, TestContext.Current.CancellationToken);
                Assert.Equal(TimeSpan.MaxValue, updated.Value.AutoDeleteOnIdle);
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, queue);
                await AzureServiceBusCommon.DeleteTopic(host, topic);
                await AzureServiceBusCommon.DeleteQueue(host, updatedQueue);
            }
        }
        [Fact(Timeout = 120000)]
        public async Task TestRegistrationRules()
        {
            var commandTopic = MessageTest.NewTopic("Rules");
            var eventTopic = MessageTest.NewTopic("RulesEvent");
            var serializer = new ZerraByteSerializer();
            try
            {
                await using var consumer = new AzureServiceBusConsumer(host, serializer, null, null, new TestLogger(), null);
                await using var producer = new AzureServiceBusProducer(host, serializer, null, null, new TestLogger(), null);
                await MessageEdgeTest.TestRegistrationRules(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
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
            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, environment))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, environment))
                {
                    await MessageEdgeTest.TestRoundTrip(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, $"{environment}_{commandTopic}");
                await AzureServiceBusCommon.DeleteTopic(host, $"{environment}_{eventTopic}");
            }
            Assert.Equal(0, log.Errors);
        }

        [Fact(Timeout = 300000)]
        public async Task TestReceiveLimitAwaited()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageEdgeTest.TestReceiveLimitAwaited(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestMalformedMessages()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            await using var client = new Azure.Messaging.ServiceBus.ServiceBusClient(host);
            async Task Send(string queueOrTopic, byte[] body)
            {
                await using var sender = client.CreateSender(queueOrTopic);
                await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage(body));
            }
            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null))
                {
                    await MessageEdgeTest.TestMalformedMessages(producer, producer, consumer, consumer, commandTopic, eventTopic, serializer, log,
                        (type, data, source) => serializer.SerializeBytes(new AzureServiceBusMessage { MessageType = type, MessageData = data, Source = source }),
                        body => Send(commandTopic, body),
                        body => Send(eventTopic, body),
                        null,
                        TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                await AzureServiceBusCommon.DeleteTopic(host, eventTopic);
            }
        }

        private static string AckQueue(AzureServiceBusProducer producer) => (string)typeof(AzureServiceBusProducer).GetField("ackQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producer)!;
        private static ICollection<string> PendingAckKeys(AzureServiceBusProducer producer) => ((System.Collections.IDictionary)typeof(AzureServiceBusProducer).GetField("ackCallbacks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producer)!).Keys.Cast<string>().ToArray();

        [Fact(Timeout = 300000)]
        public async Task TestInvalidAcknowledgement()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var client = new Azure.Messaging.ServiceBus.ServiceBusClient(host);
            try
            {
                await using var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null);
                await using var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null);
                try
                {
                    Zerra.Reflection.TypeFinder.Register(typeof(TestCommand));
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
                    await using (var sender = client.CreateSender(AckQueue(producer)))
                    {
                        await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage(new byte[] { 1, 2, 3 }) { SessionId = key }, TestContext.Current.CancellationToken);
                        //an acknowledgement nobody is waiting for is ignored
                        await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage(new byte[] { 1 }) { SessionId = "unknown" }, TestContext.Current.CancellationToken);
                    }
                    _ = await Assert.ThrowsAnyAsync<Exception>(() => awaiting.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken));
                }
                finally
                {
                    //disposing the consumer waits for the handler
                    release.TrySetResult();
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestAckQueueDeleted()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            string? ackQueue = null;
            try
            {
                await using var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, null);
                await using var producer = new AzureServiceBusProducer(host, serializer, null, null, log, null);
                ackQueue = AckQueue(producer);
                Zerra.Reflection.TypeFinder.Register(typeof(TestCommand));
                ((ICommandConsumer)consumer).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
                ((ICommandConsumer)consumer).RegisterCommandType(4, commandTopic, typeof(TestCommand));
                ((ICommandProducer)producer).RegisterCommandType(4, commandTopic, typeof(TestCommand));
                ((ICommandConsumer)consumer).Open();

                await ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand { ID = Guid.NewGuid() }, "test", TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

                //deleted outside Zerra while the producer listens on it, awaited commands still get their acknowledgements
                await AzureServiceBusCommon.CreateAdministrationClient(host).DeleteQueueAsync(ackQueue, TestContext.Current.CancellationToken);
                await ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand { ID = Guid.NewGuid() }, "test", TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(120), TestContext.Current.CancellationToken);
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
                if (ackQueue is not null)
                    await AzureServiceBusCommon.DeleteQueue(host, ackQueue);
            }
        }

        [Fact(Timeout = 120000)]
        public async Task TestLongNamesTruncated()
        {
            //nothing is created until a consumer opens or a producer sends
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            var environment = new string('e', 300);
            await using (var producer = new AzureServiceBusProducer(host, serializer, null, null, log, environment))
            await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, null, log, environment))
            {
                ((ICommandProducer)producer).RegisterCommandType(1, "topic", typeof(TestCommand));
                ((IEventProducer)producer).RegisterEventType(1, "topic", typeof(TestEvent));
                ((ICommandConsumer)consumer).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
                ((IEventConsumer)consumer).Setup("EdgeService", (_, _) => Task.CompletedTask);
                ((ICommandConsumer)consumer).RegisterCommandType(1, "topic", typeof(TestCommand));
                ((IEventConsumer)consumer).RegisterEventType(1, "topic", typeof(TestEvent), EventConsumerMode.PerService);
            }
            //the producer's command queue and event topic, and the consumer's
            Assert.True(log.Warnings >= 4, $"{log.Warnings} warnings");
        }
    }
}
