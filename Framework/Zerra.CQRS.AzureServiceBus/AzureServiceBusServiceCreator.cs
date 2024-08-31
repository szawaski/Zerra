// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.AzureServiceBus
{
    public class AzureServiceBusServiceCreator : IServiceCreator
    {
        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string? environment;
        public AzureServiceBusServiceCreator(IServiceCreator serviceCreatorForQueries, string? environment)
        {
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new AzureServiceBusProducer(messageHost, symmetricConfig, environment);
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new AzureServiceBusConsumer(messageHost, symmetricConfig, environment);
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new AzureServiceBusProducer(messageHost, symmetricConfig, environment);
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return new AzureServiceBusConsumer(messageHost, symmetricConfig, environment);
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
