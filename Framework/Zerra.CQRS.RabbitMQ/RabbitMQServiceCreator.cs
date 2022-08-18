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
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQProducer> rabbitClients = new ConcurrentFactoryDictionary<string, RabbitMQProducer>();

        private readonly string rabbitHost;
        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string environment;
        public RabbitMQServiceCreator(string rabbitHost, IServiceCreator serviceCreatorForQueries, string environment)
        {
            this.rabbitHost = rabbitHost;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQProducer(host, encryptionKey, environment));
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQConsumer(host, encryptionKey, environment));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQProducer(host, encryptionKey, environment));
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQConsumer(host, encryptionKey, environment));
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
