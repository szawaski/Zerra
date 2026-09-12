// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using System;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.Web
{
    public sealed class KestrelServiceCreator : IServiceCreator
    {
        private readonly IApplicationBuilder? applicationBuilder;
        private readonly KestrelCqrsServerLinkedSettings settings;
        private bool middlewareAdded;
        public KestrelServiceCreator(IApplicationBuilder? applicationBuilder = null, string? route = null, ContentType contentType = ContentType.Bytes, ICqrsAuthorizer? authorizer = null)
        {
            this.applicationBuilder = applicationBuilder;
            this.settings = new KestrelCqrsServerLinkedSettings(route, authorizer, contentType);
            this.middlewareAdded = false;
        }

        //the middleware is built here instead of UseMiddleware because that resolves the constructor by argument type and a null symmetricConfig matches nothing
        private void AddMiddleware(SymmetricConfig? symmetricConfig)
        {
            if (middlewareAdded)
                return;
            middlewareAdded = true;
            _ = applicationBuilder!.Use(next =>
            {
                var middleware = new KestrelCqrsServerMiddleware(next, symmetricConfig, settings);
                return middleware.Invoke;
            });
        }

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                throw new Exception($"{nameof(KestrelServiceCreator)}.{nameof(CreateCommandProducer)} requires {nameof(messageHost)}");
            return new KestrelCqrsClient(messageHost, settings.ContentType, symmetricConfig, settings.Authorizer, settings.Route);
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (applicationBuilder is null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateCommandConsumer)}");

            AddMiddleware(symmetricConfig);
            return new KestrelCqrsServerCommandConsumer(settings);
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                throw new Exception($"{nameof(KestrelServiceCreator)}.{nameof(CreateCommandProducer)} requires {nameof(messageHost)}");
            return new KestrelCqrsClient(messageHost, settings.ContentType, symmetricConfig, settings.Authorizer, settings.Route);
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (applicationBuilder is null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateCommandConsumer)}");

            AddMiddleware(symmetricConfig);
            return new KestrelCqrsServerEventConsumer(settings);
        }

        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                throw new Exception($"{nameof(KestrelServiceCreator)}.{nameof(CreateQueryClient)} requires {nameof(serviceUrl)}");
            return new KestrelCqrsClient(serviceUrl, settings.ContentType, symmetricConfig, settings.Authorizer, settings.Route);
        }

        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (applicationBuilder is null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateQueryServer)}");

            AddMiddleware(symmetricConfig);
            return new KestrelCqrsServerQueryServer(settings);
        }
    }
}
