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
    /// <summary>
    /// A minimal HTTP protocol for communication between services.
    /// This uses JSON with the <see cref="Zerra.Serialization.Json.JsonSerializer"/>
    /// Used in <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/>.
    /// </summary>
    public class HttpServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, HttpCqrsServer> servers = new();

        private readonly ContentType contentType;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string[]? allowOrigins;

        /// <inheritdoc />
        public HttpServiceCreator(ContentType contentType = ContentType.Json, ICqrsAuthorizer? authorizer = null, string[]? allowOrigins = null)
        {
            this.contentType = contentType;
            this.authorizer = authorizer;
            this.allowOrigins = allowOrigins;
        }

        /// <inheritdoc />
        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new HttpCqrsClient(contentType, messageHost, symmetricConfig, authorizer);
        }

        /// <inheritdoc />
        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, contentType, symmetricConfig, authorizer, allowOrigins, static (messageHost, contentType, symmetricConfig, authorizer, allowOrigins) => new HttpCqrsServer(contentType, messageHost, symmetricConfig, authorizer, allowOrigins));
        }

        /// <inheritdoc />
        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new HttpCqrsClient(contentType, messageHost, symmetricConfig, authorizer);
        }

        /// <inheritdoc />
        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, contentType, symmetricConfig, authorizer, allowOrigins, static (messageHost, contentType, symmetricConfig, authorizer, allowOrigins) => new HttpCqrsServer(contentType, messageHost, symmetricConfig, authorizer, allowOrigins));
        }

        /// <inheritdoc />
        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return new HttpCqrsClient(contentType, serviceUrl, symmetricConfig, authorizer);
        }

        /// <inheritdoc />
        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return servers.GetOrAdd(serviceUrl, contentType, symmetricConfig, authorizer, allowOrigins, static (serviceUrl, contentType, symmetricConfig, authorizer, allowOrigins) => new HttpCqrsServer(contentType, serviceUrl, symmetricConfig, authorizer, allowOrigins));
        }
    }
}
