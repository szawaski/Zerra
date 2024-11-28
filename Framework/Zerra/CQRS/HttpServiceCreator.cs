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
        private static readonly ConcurrentFactoryDictionary<string, HttpCqrsServer> servers = new();

        private readonly ContentType contentType;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string[]? allowOrigins;
        public HttpServiceCreator(ContentType contentType = ContentType.Json, ICqrsAuthorizer? authorizer = null, string[]? allowOrigins = null)
        {
            this.contentType = contentType;
            this.authorizer = authorizer;
            this.allowOrigins = allowOrigins;
        }

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new HttpCqrsClient(contentType, messageHost, symmetricConfig, authorizer);
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, contentType, symmetricConfig, authorizer, allowOrigins, static (messageHost, contentType, symmetricConfig, authorizer, allowOrigins) => new HttpCqrsServer(contentType, messageHost, symmetricConfig, authorizer, allowOrigins));
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new HttpCqrsClient(contentType, messageHost, symmetricConfig, authorizer);
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, contentType, symmetricConfig, authorizer, allowOrigins, static (messageHost, contentType, symmetricConfig, authorizer, allowOrigins) => new HttpCqrsServer(contentType, messageHost, symmetricConfig, authorizer, allowOrigins));
        }

        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return new HttpCqrsClient(contentType, serviceUrl, symmetricConfig, authorizer);
        }

        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return servers.GetOrAdd(serviceUrl, contentType, symmetricConfig, authorizer, allowOrigins, static (serviceUrl, contentType, symmetricConfig, authorizer, allowOrigins) => new HttpCqrsServer(contentType, serviceUrl, symmetricConfig, authorizer, allowOrigins));
        }
    }
}
