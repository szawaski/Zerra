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
        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string? environment;
        private readonly string? userName;
        private readonly string? password;
        public KafkaServiceCreator(IServiceCreator serviceCreatorForQueries, string? environment, string? userName = null, string? password = null)
        {
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
            this.userName = userName;
            this.password = password;
        }

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new KafkaProducer(messageHost, symmetricConfig, environment, userName, password);
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new KafkaConsumer(messageHost, symmetricConfig, environment, userName, password);
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new KafkaProducer(messageHost, symmetricConfig, environment, userName, password);
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new KafkaConsumer(messageHost, symmetricConfig, environment, userName, password);
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
