// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Collections;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS.Kafka
{
    public class KafkaServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, KafkaServer> kafkaServers = new ConcurrentFactoryDictionary<string, KafkaServer>();
        private static readonly ConcurrentFactoryDictionary<string, KafkaClient> kakfaClients = new ConcurrentFactoryDictionary<string, KafkaClient>();

        private readonly string host;
        private readonly IServiceCreator serviceCreatorForQueries;
        public KafkaServiceCreator(string host, IServiceCreator serviceCreatorForQueries)
        {
            this.host = host;
            this.serviceCreatorForQueries = serviceCreatorForQueries;
        }

        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaClient(host, encryptionKey));
        }

        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaServer(host, encryptionKey));
        }

        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaClient(host, encryptionKey));
        }

        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaServer(host, encryptionKey));
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
