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
        private static readonly ConcurrentFactoryDictionary<string, AzureServiceBusConsumer> azureServiceBusServers = new();
        private static readonly ConcurrentFactoryDictionary<string, AzureServiceBusProducer> azureServiceBusProducers = new();

        private readonly string host;
        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string? environment;
        public AzureServiceBusServiceCreator(string host, IServiceCreator serviceCreatorForQueries, string? environment)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer? CreateCommandProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return azureServiceBusProducers.GetOrAdd(host, (host) => new AzureServiceBusProducer(host, symmetricConfig, environment));
        }

        public ICommandConsumer? CreateCommandConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return azureServiceBusServers.GetOrAdd(host, (host) => new AzureServiceBusConsumer(host, symmetricConfig, environment));
        }

        public IEventProducer? CreateEventProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return azureServiceBusProducers.GetOrAdd(host, (host) => new AzureServiceBusProducer(host, symmetricConfig, environment));
        }

        public IEventConsumer? CreateEventConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return azureServiceBusServers.GetOrAdd(host, (host) => new AzureServiceBusConsumer(host, symmetricConfig, environment));
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
