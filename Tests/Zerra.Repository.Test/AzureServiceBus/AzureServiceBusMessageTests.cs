// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS;
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
            Assert.True(await AzureServiceBusConnection.TestAsync(host, null));
            //not the emulator, so its management port isn't substituted, and nothing listens on port 1
            Assert.False(await AzureServiceBusConnection.TestAsync("Endpoint=sb://localhost:1;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;", TimeSpan.FromSeconds(2)));
        }

        [Fact(Timeout = 300000)]
        public async Task TestSequence()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var log = new TestLogger();

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, encryptor, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, encryptor, log, null))
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

        [Fact(Timeout = 300000)]
        public async Task TestCommandSentBeforeConsumer()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, null, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, null, log, null))
                {
                    await MessageTest.TestCommandSentBeforeConsumer(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await AzureServiceBusCommon.DeleteQueue(host, commandTopic);
            }
        }

        [Fact(Timeout = 120000)]
        public async Task TestAckListenerStartsOnRegister()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            string? ackQueue = null;

            try
            {
                await using (var producer = new AzureServiceBusProducer(host, new ZerraByteSerializer(), null, new TestLogger(), null))
                {
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
                }
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
                await using (var consumer = new AzureServiceBusConsumer(host, serializer, encryptor, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, encryptor, log, null))
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
                await using (var serviceAReplica1 = new AzureServiceBusConsumer(host, serializer, encryptor, log, null))
                await using (var serviceAReplica2 = new AzureServiceBusConsumer(host, serializer, encryptor, log, null))
                await using (var serviceB = new AzureServiceBusConsumer(host, serializer, encryptor, log, null))
                await using (var producer = new AzureServiceBusProducer(host, serializer, encryptor, log, null))
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
    }
}
