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
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQConsumer> rabbitServers = new();
        private static readonly ConcurrentFactoryDictionary<string, RabbitMQProducer> rabbitClients = new();

        private readonly IServiceCreator serviceCreatorForQueries;
        private readonly string? environment;
        public RabbitMQServiceCreator(IServiceCreator serviceCreatorForQueries, string? environment)
        {
            this.serviceCreatorForQueries = serviceCreatorForQueries;
            this.environment = environment;
        }

        public ICommandProducer? CreateCommandProducer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return rabbitClients.GetOrAdd(serviceUrl, (host) => new RabbitMQProducer(host, symmetricConfig, environment));
        }

        public ICommandConsumer? CreateCommandConsumer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return rabbitServers.GetOrAdd(serviceUrl, (host) => new RabbitMQConsumer(host, symmetricConfig, environment));
        }

        public IEventProducer? CreateEventProducer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return rabbitClients.GetOrAdd(serviceUrl, (host) => new RabbitMQProducer(host, symmetricConfig, environment));
        }

        public IEventConsumer? CreateEventConsumer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return rabbitServers.GetOrAdd(serviceUrl, (host) => new RabbitMQConsumer(host, symmetricConfig, environment));
        }

        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryClient(serviceUrl, symmetricConfig);
        }

        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueries.CreateQueryServer(serviceUrl, symmetricConfig);
        }
    }
}
