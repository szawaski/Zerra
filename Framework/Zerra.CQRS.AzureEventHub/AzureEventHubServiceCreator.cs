﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.AzureEventHub
{
    public sealed class AzureEventHubServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, AzureEventHubConsumer> azureEventHubServers = new();
        private static readonly ConcurrentFactoryDictionary<string, AzureEventHubProducer> azureEventHubClients = new();

        private readonly string host;
        private readonly string eventHubName;
        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string environment;
        public AzureEventHubServiceCreator(string host, string eventHubName, IServiceCreator serviceCreatorForQueries, string environment)
        {
            this.host = host;
            this.eventHubName = eventHubName;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return azureEventHubClients.GetOrAdd(host, (host) => new AzureEventHubProducer(host, eventHubName, symmetricConfig, environment));
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return azureEventHubServers.GetOrAdd(host, (host) => new AzureEventHubConsumer(host, eventHubName, symmetricConfig, environment));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return azureEventHubClients.GetOrAdd(host, (host) => new AzureEventHubProducer(host, eventHubName, symmetricConfig, environment));
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return azureEventHubServers.GetOrAdd(host, (host) => new AzureEventHubConsumer(host, eventHubName, symmetricConfig, environment));
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
