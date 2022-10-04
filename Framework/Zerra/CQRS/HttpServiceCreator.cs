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

        private readonly ContentType contentType;
        private readonly ICQRSAuthorizer authorizer;
        private readonly string[] allowOrigins;
        public HttpServiceCreator(ContentType contentType = ContentType.Json, ICQRSAuthorizer authorizer = null, string[] allowOrigins = null)
        {
            this.contentType = contentType;
            this.authorizer = authorizer;
            this.allowOrigins = allowOrigins;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return new HttpCQRSClient(contentType, serviceUrl, authorizer);
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return servers.GetOrAdd(serviceUrl, (url) => HttpCQRSServer.CreateDefault(url, authorizer, allowOrigins));
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(HttpServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(HttpServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return new HttpCQRSClient(contentType, serviceUrl, authorizer);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return servers.GetOrAdd(serviceUrl, (url) => HttpCQRSServer.CreateDefault(url, authorizer, allowOrigins));
        }
    }
}
