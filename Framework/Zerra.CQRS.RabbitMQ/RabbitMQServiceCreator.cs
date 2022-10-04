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

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQProducer(host, symmetricConfig, environment));
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQConsumer(host, symmetricConfig, environment));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMQProducer(host, symmetricConfig, environment));
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMQConsumer(host, symmetricConfig, environment));
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
