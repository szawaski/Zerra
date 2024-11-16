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
        private readonly string? environment;
        private readonly string? userName;
        private readonly string? password;
        public KafkaServiceCreator(IServiceCreator serviceCreatorForQueries, string? environment, string? userName = null, string? password = null)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
            this.userName = userName;
            this.password = password;
        }

        public ICommandProducer? CreateCommandProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return new KafkaProducer(messageHost, symmetricConfig, environment, userName, password);
        }

        public ICommandConsumer? CreateCommandConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return new KafkaConsumer(messageHost, symmetricConfig, environment, userName, password);
        }

        public IEventProducer? CreateEventProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return new KafkaProducer(messageHost, symmetricConfig, environment, userName, password);
        }

        public IEventConsumer? CreateEventConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return new KafkaConsumer(messageHost, symmetricConfig, environment, userName, password);
        }

        public IQueryClient? CreateQueryClient(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryClient(serviceUrl, symmetricConfig);
        }

        public IQueryServer? CreateQueryServer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryServer(serviceUrl, symmetricConfig);
        }
    }
}
