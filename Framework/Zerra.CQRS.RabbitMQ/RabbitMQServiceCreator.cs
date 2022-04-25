// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.RabbitMQ
{
    public class RabbitMQServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQConsumer> rabbitServers = new ConcurrentFactoryDictionary<string, RabbitMQConsumer>();
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQClient> rabbitClients = new ConcurrentFactoryDictionary<string, RabbitMQClient>();

        private readonly string rabbitHost;
        private readonly IServiceCreator serviceCreatorForQueries;
        public RabbitMQServiceCreator(string rabbitHost, IServiceCreator serviceCreatorForQueries)
        {
            this.rabbitHost = rabbitHost;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQClient(host, encryptionKey));
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQConsumer(host, encryptionKey));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQClient(host, encryptionKey));
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQConsumer(host, encryptionKey));
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
