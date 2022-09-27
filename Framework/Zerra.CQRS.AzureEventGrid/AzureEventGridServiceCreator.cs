// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.AzureEventGrid
{
    public class AzureEventGridServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, AzureEventGridConsumer> AzureEventGridServers = new();
        private static readonly ConcurrentFactoryDictionary<string, AzureEventGridProducer> kakfaClients = new();

        private readonly string host;
        private readonly IServiceCreator serviceCreatorForQueries;
        public AzureEventGridServiceCreator(string host, IServiceCreator serviceCreatorForQueries)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new AzureEventGridProducer(host, encryptionKey));
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return AzureEventGridServers.GetOrAdd(host, (host) => new AzureEventGridConsumer(host, encryptionKey));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new AzureEventGridProducer(host, encryptionKey));
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return AzureEventGridServers.GetOrAdd(host, (host) => new AzureEventGridConsumer(host, encryptionKey));
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
