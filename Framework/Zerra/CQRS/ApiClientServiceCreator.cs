// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS
{
    /// <summary>
    /// Protocol to communicate with externally exposed API services using <see cref="Zerra.Web.CqrsApiGatewayMiddleware"/>.
    /// This uses HTTP and is assumed to be over a public network.
    /// Used in <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/>.
    /// </summary>
    public sealed class ApiClientServiceCreator : IServiceCreator
    {
        private readonly ContentType contentType = ContentType.Json;
        private readonly IServiceCreator serviceCreatorForQueriesAndConsumers;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string? route;

        /// <summary>
        /// Creates a new API Client Service Factory.
        /// Query Servers and Message Consumers will be from another service creator.
        /// This Service Creator will supply the Query Client and Message Producers.
        /// </summary>
        /// <param name="serviceCreatorForQueriesAndConsumers">Another service creator that this one will wrap.  Query Servers and Message Consumers of this service creator will be used while the others are created by this service.</param>
        /// <param name="authorizer">An authorizor for security enforcement.</param>
        /// <param name="route">Adds a route at the end of the URL.</param>
        public ApiClientServiceCreator(IServiceCreator serviceCreatorForQueriesAndConsumers, ICqrsAuthorizer? authorizer, string? route)
        {
            this.serviceCreatorForQueriesAndConsumers = serviceCreatorForQueriesAndConsumers;
            this.authorizer = authorizer;
            this.route = route;
        }

        /// <inheritdoc />
        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            if (symmetricConfig is not null)
                throw new ArgumentException($"Encryption not supported for {nameof(ApiClient)}", nameof(symmetricConfig));
            return new ApiClient(messageHost, contentType, authorizer, route);
        }

        /// <inheritdoc />
        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueriesAndConsumers.CreateCommandConsumer(messageHost, symmetricConfig);
        }

        /// <inheritdoc />
        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            if (symmetricConfig is not null)
                throw new ArgumentException($"Encryption not supported for {nameof(ApiClient)}", nameof(symmetricConfig));
            return new ApiClient(messageHost, contentType, authorizer, route);
        }

        /// <inheritdoc />
        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueriesAndConsumers.CreateEventConsumer(messageHost, symmetricConfig);
        }

        /// <inheritdoc />
        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            if (symmetricConfig is not null)
                throw new ArgumentException($"Encryption not supported for {nameof(ApiClient)}", nameof(symmetricConfig));
            return new ApiClient(serviceUrl, contentType, authorizer, route);
        }

        /// <inheritdoc />
        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueriesAndConsumers.CreateQueryServer(serviceUrl, symmetricConfig);
        }
    }
}
