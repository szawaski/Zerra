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
    public class TcpInternalServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCQRSServer> servers = new();

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return TcpRawCQRSClient.CreateDefault(serviceUrl, encryptionKey);
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => TcpRawCQRSServer.CreateDefault(url, encryptionKey));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(TcpInternalServiceCreator)} does not support {nameof(CreateEventClient)}");
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(TcpInternalServiceCreator)} does not support {nameof(CreateEventServer)}");
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
