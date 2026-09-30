// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Confluent.Kafka;
using Xunit;
using Zerra.CQRS;
using Zerra.Compression;
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
            var compressor = new ZerraCompressor(CompressionAlgorithmType.Brotli);
            var log = new TestLogger();
            string? ackTopic = null;

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, encryptor, compressor, log, null, null, null))
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

        [Fact(Timeout = 120000)]
        public async Task TestAckListenerStartsOnRegister()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            string? ackTopic = null;

            try
            {
                using (var producer = new KafkaProducer(host, new ZerraByteSerializer(), null, null, new TestLogger(), null, null, null))
                {
                    ackTopic = AckTopic(producer);
                    ((ICommandProducer)producer).RegisterCommandType(1, commandTopic, typeof(TestCommand));

                    //registering starts the listener, the first command doesn't wait for the topic
                    await WaitUntilTopicExists(ackTopic);
                }
            }
            finally
            {
                if (ackTopic is not null)
                    await KafkaCommon.DeleteTopic(host, null, null, ackTopic);
            }
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
                await ((ICommandProducer)producer).DispatchAwaitAsync(new TestCommand() { ID = Guid.NewGuid() }, "Zerra.Repository.Test", TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(90), TestContext.Current.CancellationToken);
            }

            try
            {
                using (var consumer = new KafkaConsumer(host, serializer, null, null, log, null, null, null))
                {
                    var commandConsumer = (ICommandConsumer)consumer;
                    commandConsumer.Setup(new CommandCounter(), (command, source, cancellationToken) => Task.CompletedTask, (command, source, cancellationToken) => Task.CompletedTask, (command, source, cancellationToken) => Task.FromResult<object?>(null));
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
            var replicas = Enumerable.Range(0, 8).Select(_ => new KafkaCommonHost(host, null, null)).ToArray();
            try
            {
                await Task.WhenAll(replicas.Select(x => KafkaCommon.EnsureTopic(x, topic).AsTask()));
                await WaitUntilTopicExists(topic);
            }
            finally
            {
                foreach (var replica in replicas)
                    replica.Client.Dispose();
                await KafkaCommon.DeleteTopic(host, null, null, topic);
            }
        }

        private static string AckTopic(KafkaProducer producer) => (string)typeof(KafkaProducer).GetField("ackTopic", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(producer)!;

        private static async Task<bool> ConsumerGroupExists(string group)
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
            var groups = await admin.ListConsumerGroupsAsync();
            return groups.Valid.Any(x => x.GroupId == group);
        }

        //lists every topic, asking for one by name could auto-create it
        private static async Task WaitUntilTopicExists(string topic)
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig() { BootstrapServers = host }).Build();
            for (var attempt = 1; ; attempt++)
            {
                if (admin.GetMetadata(TimeSpan.FromSeconds(10)).Topics.Any(x => x.Topic == topic && x.Error.Code == ErrorCode.NoError))
                    return;
                if (attempt == 60)
                    throw new TimeoutException($"Topic {topic} was not created");
                await Task.Delay(500);
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
