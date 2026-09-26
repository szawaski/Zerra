// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Kafka;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Repository.Test.Kafka
{
    public class KafkaMessageTests
    {
        private const string host = "localhost:9092";

        [Fact]
        public async Task TestConnection()
        {
            Assert.True(await KafkaConnection.TestAsync(host, null, null));
            //nothing listens on port 1
            Assert.False(await KafkaConnection.TestAsync("localhost:1", null, null, TimeSpan.FromSeconds(2)));
        }

        [Fact(Timeout = 300000)]
        public async Task TestSequence()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, encryptor, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, encryptor, log, null, null, null))
                {
                    ackTopic = producer.AckTopic;
                    await MessageTest.TestSequence(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
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
                using (var consumer = new KafkaConsumer(host, serializer, encryptor, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, encryptor, log, null, null, null))
                {
                    ackTopic = producer.AckTopic;
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
                using (var serviceAReplica1 = new KafkaConsumer(host, serializer, encryptor, log, null, null, null))
                using (var serviceAReplica2 = new KafkaConsumer(host, serializer, encryptor, log, null, null, null))
                using (var serviceB = new KafkaConsumer(host, serializer, encryptor, log, null, null, null))
                using (var producer = new KafkaProducer(host, serializer, encryptor, log, null, null, null))
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

        private static async Task Cleanup(string commandTopic, string eventTopic, string? ackTopic)
        {
            await KafkaCommon.DeleteTopic(host, null, null, commandTopic);
            await KafkaCommon.DeleteTopic(host, null, null, eventTopic);

            //command consumers share a group named after the topic, so it outlives them, event consumer groups are deleted by the consumers themselves
            await DeleteConsumerGroup(commandTopic);

            //the producer deletes its acknowledgement topic and group when it's disposed, this catches them if that didn't finish
            if (ackTopic is not null)
            {
                await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
                await DeleteConsumerGroup(ackTopic);
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
