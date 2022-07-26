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
    public class HttpServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, HttpCQRSServer> servers = new();

        private readonly NetworkType networkType;
        private readonly ContentType contentType;
        private readonly IHttpAuthorizer httpAuthorizer;
        private readonly string[] allowOrigins;
        public HttpServiceCreator(NetworkType networkType = NetworkType.Internal, ContentType contentType = ContentType.Json, IHttpAuthorizer httpAuthorizer = null, string[] allowOrigins = null)
        {
            this.httpAuthorizer = httpAuthorizer;
            this.allowOrigins = allowOrigins;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return new HttpCQRSClient(networkType, contentType, serviceUrl, httpAuthorizer);
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => HttpCQRSServer.CreateDefault(url, httpAuthorizer, allowOrigins));
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(HttpServiceCreator)} does not support {nameof(CreateEventClient)}");
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(HttpServiceCreator)} does not support {nameof(CreateEventServer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return new HttpCQRSClient(networkType, contentType, serviceUrl, httpAuthorizer);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return servers.GetOrAdd(serviceUrl, (url) => HttpCQRSServer.CreateDefault(url, httpAuthorizer, allowOrigins));
        }
    }
}
