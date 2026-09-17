// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using RabbitMQ.Client;
using Xunit;
using Zerra.CQRS.RabbitMQ;
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
            }
        }

        //the consumer declares an exchange per topic, which outlives the connection, its queues are exclusive and go with the connection
        private static void DeleteExchanges(string commandTopic, string eventTopic)
        {
            var factory = RabbitMQCommon.CreateConnectionFactory(host);
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDelete(commandTopic, false);
            channel.ExchangeDelete(eventTopic, false);
            channel.Close();
        }
    }
}
