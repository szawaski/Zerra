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

        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string? environment;
        public KafkaServiceCreator(IServiceCreator serviceCreatorForQueries, string? environment)
        {
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return kakfaClients.GetOrAdd(messageHost, (host) => new KafkaProducer(host, symmetricConfig, environment));
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return kafkaServers.GetOrAdd(messageHost, (host) => new KafkaConsumer(host, symmetricConfig, environment));
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return kakfaClients.GetOrAdd(messageHost, (host) => new KafkaProducer(host, symmetricConfig, environment));
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return kafkaServers.GetOrAdd(messageHost, (host) => new KafkaConsumer(host, symmetricConfig, environment));
        }

        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryClient(serviceUrl, symmetricConfig);
        }

        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryServer(serviceUrl, symmetricConfig);
        }
    }
}
