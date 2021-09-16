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
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCQRSServer> servers = new ConcurrentFactoryDictionary<string, TcpRawCQRSServer>();
        private static readonly ConcurrentFactoryDictionary<string, RabbitMessageServer> rabbitServers = new ConcurrentFactoryDictionary<string, RabbitMessageServer>();
        private static readonly ConcurrentFactoryDictionary<string, RabbitMessageClient> rabbitClients = new ConcurrentFactoryDictionary<string, RabbitMessageClient>();

        private readonly string rabbitHost;
        public RabbitServiceCreator(string rabbitHost)
        {
            this.rabbitHost = rabbitHost;
        }

        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMessageClient(host, encryptionKey));
        }

        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMessageServer(host, encryptionKey));
        }

        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitClients.GetOrAdd(rabbitHost, (host) => new RabbitMessageClient(host, encryptionKey));
        }

        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return rabbitServers.GetOrAdd(rabbitHost, (host) => new RabbitMessageServer(host, encryptionKey));
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
