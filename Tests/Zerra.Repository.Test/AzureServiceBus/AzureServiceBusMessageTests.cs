// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
using Zerra.Compression;
using Zerra.CQRS.AzureServiceBus;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Repository.Test.AzureServiceBus
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

        [Fact(Timeout = 300000)]
        public async Task TestSequence()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var compressor = new ZerraCompressor(CompressionAlgorithmType.Brotli);
            var log = new TestLogger();

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, encryptor, compressor, log, null))
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
    }
}
