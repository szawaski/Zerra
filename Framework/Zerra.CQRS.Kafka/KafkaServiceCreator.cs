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
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCQRSServer> servers = new ConcurrentFactoryDictionary<string, TcpRawCQRSServer>();
        private static readonly ConcurrentFactoryDictionary<string, KafkaMessageServer> kafkaServers = new ConcurrentFactoryDictionary<string, KafkaMessageServer>();
        private static readonly ConcurrentFactoryDictionary<string, KafkaMessageClient> kakfaClients = new ConcurrentFactoryDictionary<string, KafkaMessageClient>();

        private readonly string host;
        public KafkaServiceCreator(string host)
        {
            this.host = host;
        }

        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaMessageClient(host, encryptionKey));
        }

        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaMessageServer(host, encryptionKey));
        }

        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kakfaClients.GetOrAdd(host, (host) => new KafkaMessageClient(host, encryptionKey));
        }

        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return kafkaServers.GetOrAdd(host, (host) => new KafkaMessageServer(host, encryptionKey));
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return TcpRawCQRSClient.CreateDefault(serviceUrl, encryptionKey);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => TcpRawCQRSServer.CreateDefault(url, encryptionKey));
        }
    }
}
