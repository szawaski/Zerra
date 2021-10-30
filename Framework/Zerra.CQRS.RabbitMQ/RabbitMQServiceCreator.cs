// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.RabbitMQ
{
    public class RabbitMQServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQServer> rabbitServers = new ConcurrentFactoryDictionary<string, RabbitMQServer>();
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQClient> rabbitClients = new ConcurrentFactoryDictionary<string, RabbitMQClient>();

        private readonly string rabbitHost;
        private readonly IServiceCreator serviceCreatorForQueries;
        public RabbitMQServiceCreator(string rabbitHost, IServiceCreator serviceCreatorForQueries)
        {
            this.rabbitHost = rabbitHost;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQClient(host, encryptionKey));
        }

        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQServer(host, encryptionKey));
        }

        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQClient(host, encryptionKey));
        }

        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQServer(host, encryptionKey));
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
