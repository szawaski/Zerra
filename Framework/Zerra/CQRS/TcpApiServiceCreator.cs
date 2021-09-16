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
    public class TcpApiServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, HttpCQRSServer> servers = new ConcurrentFactoryDictionary<string, HttpCQRSServer>();

        private readonly IHttpApiAuthorizer apiAuthorizer;
        private readonly string[] allowOrigins;
        public TcpApiServiceCreator(IHttpApiAuthorizer apiAuthorizer, string[] allowOrigins)
        {
            this.apiAuthorizer = apiAuthorizer;
            this.allowOrigins = allowOrigins;
        }

        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return HttpCQRSClient.CreateDefault(serviceUrl, apiAuthorizer);
        }

        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => HttpCQRSServer.CreateDefault(url, apiAuthorizer, allowOrigins));
        }

        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(TcpApiServiceCreator)} does not support {nameof(CreateEventClient)}");
        }

        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(TcpApiServiceCreator)} does not support {nameof(CreateEventServer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return HttpCQRSClient.CreateDefault(serviceUrl, apiAuthorizer);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => HttpCQRSServer.CreateDefault(url, apiAuthorizer, allowOrigins));
        }
    }
}
