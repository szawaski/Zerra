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
        private static readonly ConcurrentFactoryDictionary<string, AzureEventHubConsumer> azureEventHubServers = new ConcurrentFactoryDictionary<string, AzureEventHubConsumer>();
        private static readonly ConcurrentFactoryDictionary<string, AzureEventHubProducer> azureEventHubClients = new ConcurrentFactoryDictionary<string, AzureEventHubProducer>();

        private readonly string host;
        private readonly string eventHubName;
        private readonly IServiceCreator serviceCreatorForQueries;
        public AzureEventHubServiceCreator(string host, string eventHubName, IServiceCreator serviceCreatorForQueries)
        {
            this.host = host;
            this.eventHubName = eventHubName;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubClients.GetOrAdd(host, (host) => new AzureEventHubProducer(host, eventHubName, encryptionKey));
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubServers.GetOrAdd(host, (host) => new AzureEventHubConsumer(host, eventHubName, encryptionKey));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubClients.GetOrAdd(host, (host) => new AzureEventHubProducer(host, eventHubName, encryptionKey));
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return azureEventHubServers.GetOrAdd(host, (host) => new AzureEventHubConsumer(host, eventHubName, encryptionKey));
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
