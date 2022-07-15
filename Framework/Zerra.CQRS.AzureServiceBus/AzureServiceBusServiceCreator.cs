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
        private static readonly ConcurrentFactoryDictionary<string, AzureServiceBusConsumer> AzureServiceBusServers = new ConcurrentFactoryDictionary<string, AzureServiceBusConsumer>();
        private static readonly ConcurrentFactoryDictionary<string, AzureServiceBusProducer> kakfaClients = new ConcurrentFactoryDictionary<string, AzureServiceBusProducer>();

        private readonly string host;
        private readonly IServiceCreator serviceCreatorForQueries;
        public AzureServiceBusServiceCreator(string host, IServiceCreator serviceCreatorForQueries)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new AzureServiceBusProducer(host, encryptionKey));
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return AzureServiceBusServers.GetOrAdd(host, (host) => new AzureServiceBusConsumer(host, encryptionKey));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new AzureServiceBusProducer(host, encryptionKey));
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return AzureServiceBusServers.GetOrAdd(host, (host) => new AzureServiceBusConsumer(host, encryptionKey));
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
