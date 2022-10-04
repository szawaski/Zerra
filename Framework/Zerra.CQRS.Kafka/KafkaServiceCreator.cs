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
        private readonly string environment;
        public KafkaServiceCreator(string host, IServiceCreator serviceCreatorForQueries, string environment)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaProducer(host, symmetricConfig, environment));
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaConsumer(host, symmetricConfig, environment));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaProducer(host, symmetricConfig, environment));
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaConsumer(host, symmetricConfig, environment));
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryClient(serviceUrl, symmetricConfig);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryServer(serviceUrl, symmetricConfig);
        }
    }
}
