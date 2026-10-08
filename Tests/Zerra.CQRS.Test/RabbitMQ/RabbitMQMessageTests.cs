// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Concurrent;
using RabbitMQ.Client;
using Xunit;
using Zerra.CQRS;
using Zerra.Compression;
using Zerra.CQRS.RabbitMQ;
using Zerra.Reflection;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.CQRS.Test.RabbitMQ
{
    public class RabbitMQMessageTests
    {
        private const string host = "localhost";

        [Fact]
        public void TestConnection()
        {
            Assert.True(RabbitMQConnectionTest.Test(host));
            //nothing listens on port 1
            Assert.False(RabbitMQConnectionTest.Test("amqp://guest:guest@localhost:1", TimeSpan.FromSeconds(2)));
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
                using (var consumer = new RabbitMQConsumer(host, serializer, encryptor, compressor, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, compressor, log, null))
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
        public async Task TestSharedConsumer(bool eventFirst)
        {
            var commandTopicA = MessageTest.NewTopic("CommandA");
            var commandTopicB = MessageTest.NewTopic("CommandB");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestSharedConsumer(producer, producer, consumer, commandTopicA, commandTopicB, eventTopic, eventFirst, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopicA, commandTopicB, eventTopic);
                DeleteQueues(commandTopicA, commandTopicB);
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
                using (var replica1 = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var replica2 = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestSustainedLoad(producer, producer, replica1, replica2, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
                Assert.Equal(0, log.Errors);
            }
            finally
            {
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic);
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
                using (var replica1 = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var replica2 = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestReceiveLimitHandsOff(producer, replica1, replica2, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopic);
                DeleteQueues(commandTopic);
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
                using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestCommandSentBeforeConsumer(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopic);
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
                using (var consumer = new RabbitMQConsumer(host, serializer, encryptor, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, null, log, null))
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
                using (var serviceAReplica1 = new RabbitMQConsumer(host, serializer, encryptor, null, log, null))
                using (var serviceAReplica2 = new RabbitMQConsumer(host, serializer, encryptor, null, log, null))
                using (var serviceB = new RabbitMQConsumer(host, serializer, encryptor, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, null, log, null))
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
        public async Task TestHandlerErrorNotReceivedAgain()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();

            try
            {
                using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageTest.TestHandlerErrorNotReceivedAgain(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic, $"{eventTopic}_{MessageTest.ServiceAName}");
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
                using (var consumer = new RabbitMQConsumer(host, serializer, encryptor, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, encryptor, null, log, null))
                {
                    ICommandConsumer commandConsumer = consumer;
                    IEventConsumer eventConsumer = consumer;
                    ICommandProducer commandProducer = producer;
                    IEventProducer eventProducer = producer;

                    Task handleCommand(ICommand command, string source, CancellationToken token) { commands[((TestCommand)command).ID] = true; return Task.CompletedTask; }
                    commandConsumer.Setup(null, handleCommand, handleCommand, (command, source, token) => Task.FromResult<object?>(null));
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
        [Fact(Timeout = 120000)]
        public async Task TestRegistrationRules()
        {
            var commandTopic = MessageTest.NewTopic("Rules");
            var eventTopic = MessageTest.NewTopic("RulesEvent");
            var serializer = new ZerraByteSerializer();
            try
            {
                using var consumer = new RabbitMQConsumer(host, serializer, null, null, new TestLogger(), null);
                using var producer = new RabbitMQProducer(host, serializer, null, null, new TestLogger(), null);
                await MessageEdgeTest.TestRegistrationRules(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
            }
            finally
            {
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic);
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
                using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, environment))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, environment))
                {
                    await MessageEdgeTest.TestRoundTrip(producer, producer, consumer, consumer, commandTopic, eventTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges($"{environment}_{commandTopic}", $"{environment}_{eventTopic}");
                DeleteQueues($"{environment}_{commandTopic}");
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
                using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageEdgeTest.TestReceiveLimitAwaited(producer, consumer, commandTopic, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                DeleteExchanges(commandTopic);
                DeleteQueues(commandTopic);
            }
        }

        [Fact(Timeout = 300000)]
        public async Task TestMalformedMessages()
        {
            var commandTopic = MessageTest.NewTopic("Command");
            var eventTopic = MessageTest.NewTopic("Event");
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            using var connection = RabbitMQCommon.CreateConnectionFactory(host).CreateConnection();
            using var channel = connection.CreateModel();
            Task Send(string exchange, byte[] body)
            {
                channel.BasicPublish(exchange, String.Empty, channel.CreateBasicProperties(), body);
                return Task.CompletedTask;
            }
            try
            {
                using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, null))
                using (var producer = new RabbitMQProducer(host, serializer, null, null, log, null))
                {
                    await MessageEdgeTest.TestMalformedMessages(producer, producer, consumer, consumer, commandTopic, eventTopic, serializer, log,
                        (type, data, source) => serializer.SerializeBytes(new RabbitMQMessage { MessageType = type, MessageData = data, Source = source }),
                        body => Send(commandTopic, body),
                        body => Send(eventTopic, body),
                        null,
                        TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                channel.Close();
                DeleteExchanges(commandTopic, eventTopic);
                DeleteQueues(commandTopic);
            }
        }

        [Fact(Timeout = 120000)]
        public void TestLongNamesTruncated()
        {
            //nothing is created until a consumer opens or a producer sends
            var serializer = new ZerraByteSerializer();
            var log = new TestLogger();
            var environment = new string('e', 300);
            using (var producer = new RabbitMQProducer(host, serializer, null, null, log, environment))
            using (var consumer = new RabbitMQConsumer(host, serializer, null, null, log, environment))
            {
                ((ICommandProducer)producer).RegisterCommandType(1, "topic", typeof(TestCommand));
                ((IEventProducer)producer).RegisterEventType(1, "topic", typeof(TestEvent));
                ((ICommandConsumer)consumer).Setup(null, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.CompletedTask, (_, _, _) => Task.FromResult<object?>(null));
                ((IEventConsumer)consumer).Setup("EdgeService", (_, _) => Task.CompletedTask);
                ((ICommandConsumer)consumer).RegisterCommandType(1, "topic", typeof(TestCommand));
                ((IEventConsumer)consumer).RegisterEventType(1, "topic", typeof(TestEvent), EventConsumerMode.PerService);
            }
            //the producer's command and event exchanges, and the consumer's
            Assert.True(log.Warnings >= 4, $"{log.Warnings} warnings");
        }

        private static async Task WaitUntilReceived(ICommandProducer commandProducer, IEventProducer eventProducer, ConcurrentDictionary<Guid, bool> commands, ConcurrentDictionary<Guid, bool> events, CancellationToken cancellationToken)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (; ; )
            {
                var command = new TestCommand() { ID = Guid.NewGuid() };
                var @event = new TestEvent() { ID = Guid.NewGuid() };
                await commandProducer.DispatchAsync(command, "Zerra.CQRS.Test", cancellationToken);
                await eventProducer.DispatchAsync(@event, "Zerra.CQRS.Test", cancellationToken);
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
