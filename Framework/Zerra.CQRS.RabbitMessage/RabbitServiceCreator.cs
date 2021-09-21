// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.RabbitMessage
{
    public class RabbitServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, RabbitServer> rabbitServers = new ConcurrentFactoryDictionary<string, RabbitServer>();
        private static readonly ConcurrentFactoryDictionary<string, RabbitClient> rabbitClients = new ConcurrentFactoryDictionary<string, RabbitClient>();

        private readonly string rabbitHost;
        private readonly IServiceCreator serviceCreatorForQueries;
        public RabbitServiceCreator(string rabbitHost, IServiceCreator serviceCreatorForQueries)
        {
            this.rabbitHost = rabbitHost;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitClient(host, encryptionKey));
        }

        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitServer(host, encryptionKey));
        }

        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitClient(host, encryptionKey));
        }

        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitServer(host, encryptionKey));
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
