// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.AzureServiceBus;
using Zerra.Encryption;
using Zerra.Serialization;

namespace Zerra.Repository.Test.AzureServiceBus
{
    public class AzureServiceBusMessageTests
    {
        //the Service Bus emulator from Demo/Infrastructure, its AMQP port is moved to 5673 since RabbitMQ owns 5672
        private const string host = "Endpoint=sb://localhost:5673;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

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
    }
}
