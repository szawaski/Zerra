// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using RabbitMQ.Client;
using Xunit;
using Zerra.CQRS;
using Zerra.CQRS.RabbitMQ;
using Zerra.Reflection;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Repository.Test.RabbitMQ
{
    public class RabbitMQMessageTests
    {
        private const string host = "localhost";

        [Fact]
        public void TestConnection()
        {
            Assert.True(RabbitMQConnection.Test(host));
            //nothing listens on port 1
            Assert.False(RabbitMQConnection.Test("amqp://guest:guest@localhost:1", TimeSpan.FromSeconds(2)));
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
                using (var consumer = new RabbitMQConsumer(host, serializer, encryptor, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, log, null))
                {
                    await MessageTest.TestSequence(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic);
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
                using (var consumer = new RabbitMQConsumer(host, serializer, encryptor, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, log, null))
                {
                    await MessageTest.TestFinishesProcessingOnClose(producer, producer, consumer, consumer, commandTopic, eventTopic, disposeAsync, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic);
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
                using (var serviceAReplica1 = new RabbitMQConsumer(host, serializer, encryptor, log, null))
                using (var serviceAReplica2 = new RabbitMQConsumer(host, serializer, encryptor, log, null))
                using (var serviceB = new RabbitMQConsumer(host, serializer, encryptor, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, log, null))
                {
                    await MessageTest.TestEventConsumerModePerService(producer, serviceAReplica1, serviceAReplica2, serviceB, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(eventTopic);
                DeleteQueues($"{eventTopic}_{MessageTest.ServiceAName}", $"{eventTopic}_{MessageTest.ServiceBName}");
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestConsumesAgainAfterQueueDeleted()
        {
            const string serviceName = "ZerraTestService";
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
            var log = new TestLogger();
            var cancellationToken = TestContext.Current.CancellationToken;

            TypeFinder.Register(typeof(TestCommand));
            TypeFinder.Register(typeof(TestEvent));
            var commands = new ConcurrentDictionary<Guid, bool>();
            var events = new ConcurrentDictionary<Guid, bool>();

            try
            {
                using (var consumer = new RabbitMQConsumer(host, serializer, encryptor, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, log, null))
                {
                    ICommandConsumer commandConsumer = consumer;
                    IEventConsumer eventConsumer = consumer;
                    ICommandProducer commandProducer = producer;
                    IEventProducer eventProducer = producer;

                    Task handleCommand(ICommand command, string source, CancellationToken token) { commands[((TestCommand)command).ID] = true; return Task.CompletedTask; }
                    commandConsumer.Setup(new CommandCounter(), handleCommand, handleCommand, (command, source, token) => Task.FromResult<object?>(null));
                    commandConsumer.RegisterCommandType(10, commandTopic, typeof(TestCommand));
                    eventConsumer.Setup(serviceName, (@event, source) => { events[((TestEvent)@event).ID] = true; return Task.CompletedTask; });
                    eventConsumer.RegisterEventType(10, eventTopic, typeof(TestEvent), EventConsumerMode.PerService);
                    commandProducer.RegisterCommandType(10, commandTopic, typeof(TestCommand));
                    eventProducer.RegisterEventType(10, eventTopic, typeof(TestEvent));
                    commandConsumer.Open();
                    eventConsumer.Open();

                    await WaitUntilReceived(commandProducer, eventProducer, commands, events, cancellationToken);

                    //the broker cancels a consumer whose queue is deleted, the consumer declares the queue and consumes again
                    DeleteQueues(commandTopic, $"{eventTopic}_{serviceName}");

                    await WaitUntilReceived(commandProducer, eventProducer, commands, events, cancellationToken);

                    commandConsumer.Close();
                }
            }
            finally
            {
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic, $"{eventTopic}_{serviceName}");
            }
        }

        //a message sent before the queue is declared again is dropped, so keep sending until one of each arrives
        private static async Task WaitUntilReceived(ICommandProducer commandProducer, IEventProducer eventProducer, ConcurrentDictionary<Guid, bool> commands, ConcurrentDictionary<Guid, bool> events, CancellationToken cancellationToken)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (; ; )
            {
                var command = new TestCommand() { ID = Guid.NewGuid() };
                var @event = new TestEvent() { ID = Guid.NewGuid() };
                await commandProducer.DispatchAsync(command, "Zerra.Repository.Test", cancellationToken);
                await eventProducer.DispatchAsync(@event, "Zerra.Repository.Test", cancellationToken);
                await Task.Delay(1000, cancellationToken);
                if (commands.ContainsKey(command.ID) && events.ContainsKey(@event.ID))
                    return;
                if (timer.Elapsed > TimeSpan.FromSeconds(60))
                    throw new TimeoutException($"Not received after 60 seconds, command: {commands.ContainsKey(command.ID)}, event: {events.ContainsKey(@event.ID)}");
            }
        }

        private static void DeleteQueues(params string[] queues)
        {
            var factory = RabbitMQCommon.CreateConnectionFactory(host);
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            foreach (var queue in queues)
                _ = channel.QueueDelete(queue);
            channel.Close();
        }

        //the consumer declares an exchange per topic and the command and PerService queues, which outlive the connection, a PerReplica queue is exclusive and goes with the connection
        private static void DeleteExchanges(params string[] topics)
        {
            var factory = RabbitMQCommon.CreateConnectionFactory(host);
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            foreach (var topic in topics)
                channel.ExchangeDelete(topic, false);
            channel.Close();
        }
    }
}
