// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.Collections;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS
{
    public class TcpServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCQRSServer> servers = new();

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return TcpRawCQRSClient.CreateDefault(serviceUrl, encryptionKey);
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => TcpRawCQRSServer.CreateDefault(url, encryptionKey));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(TcpServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(TcpServiceCreator)} does not support {nameof(CreateEventConsumer)}");
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
