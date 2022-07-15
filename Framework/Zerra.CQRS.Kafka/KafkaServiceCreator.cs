// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.Kafka
{
    public class KafkaServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, KafkaConsumer> kafkaServers = new();
        private static readonly ConcurrentFactoryDictionary<string, KafkaProducer> kakfaClients = new();

        private readonly string host;
        private readonly IServiceCreator serviceCreatorForQueries;
        public KafkaServiceCreator(string host, IServiceCreator serviceCreatorForQueries)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaProducer(host, encryptionKey));
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaConsumer(host, encryptionKey));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaProducer(host, encryptionKey));
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaConsumer(host, encryptionKey));
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return serviceCreatorForQueries.CreateQueryClient(serviceUrl, encryptionKey);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return serviceCreatorForQueries.CreateQueryServer(serviceUrl, encryptionKey);
        }
    }
}
