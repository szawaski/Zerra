// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.AzureEventHub
{
    public class AzureEventHubServiceCreator : IServiceCreator
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

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubClients.GetOrAdd(host, (host) => new AzureEventHubProducer(host, eventHubName, encryptionKey, environment));
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubServers.GetOrAdd(host, (host) => new AzureEventHubConsumer(host, eventHubName, encryptionKey, environment));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubClients.GetOrAdd(host, (host) => new AzureEventHubProducer(host, eventHubName, encryptionKey, environment));
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubServers.GetOrAdd(host, (host) => new AzureEventHubConsumer(host, eventHubName, encryptionKey, environment));
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
