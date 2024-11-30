// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.CQRS
{
    public sealed class ApiClientServiceCreator : IServiceCreator
    {
        private readonly ContentType contentType = ContentType.Json;
        private readonly IServiceCreator serviceCreatorForQueriesAndConsumers;
        private readonly ICqrsAuthorizer? authorizer;
        private readonly string? route;
        public ApiClientServiceCreator(IServiceCreator serviceCreatorForQueriesAndConsumers, ICqrsAuthorizer? authorizer, string? route)
        {
            this.serviceCreatorForQueriesAndConsumers = serviceCreatorForQueriesAndConsumers;
            this.authorizer = authorizer;
            this.route = route;
        }

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            if (symmetricConfig is not null)
                throw new ArgumentException($"Encryption not supported for {nameof(ApiClient)}", nameof(symmetricConfig));
            return new ApiClient(messageHost, contentType, authorizer, route);
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueriesAndConsumers.CreateCommandConsumer(messageHost, symmetricConfig);
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            if (symmetricConfig is not null)
                throw new ArgumentException($"Encryption not supported for {nameof(ApiClient)}", nameof(symmetricConfig));
            return new ApiClient(messageHost, contentType, authorizer, route);
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueriesAndConsumers.CreateEventConsumer(messageHost, symmetricConfig);
        }

        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            if (symmetricConfig is not null)
                throw new ArgumentException($"Encryption not supported for {nameof(ApiClient)}", nameof(symmetricConfig));
            return new ApiClient(serviceUrl, contentType, authorizer, route);
        }

        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            return serviceCreatorForQueriesAndConsumers.CreateQueryServer(serviceUrl, symmetricConfig);
        }
    }
}
