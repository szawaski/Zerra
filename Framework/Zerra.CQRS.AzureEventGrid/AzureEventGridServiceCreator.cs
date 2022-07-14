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
        private static readonly ConcurrentFactoryDictionary<string, AzureEventGridConsumer> AzureEventGridServers = new ConcurrentFactoryDictionary<string, AzureEventGridConsumer>();
        private static readonly ConcurrentFactoryDictionary<string, AzureEventGridProducer> kakfaClients = new ConcurrentFactoryDictionary<string, AzureEventGridProducer>();

        private readonly string host;
        private readonly IServiceCreator serviceCreatorForQueries;
        public AzureEventGridServiceCreator(string host, IServiceCreator serviceCreatorForQueries)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new AzureEventGridProducer(host, encryptionKey));
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return AzureEventGridServers.GetOrAdd(host, (host) => new AzureEventGridConsumer(host, encryptionKey));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new AzureEventGridProducer(host, encryptionKey));
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
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
