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
        private readonly KestrelCQRSServerLinkedSettings settings;
        private bool middlewareAdded;
        public KestrelServiceCreator(IApplicationBuilder? applicationBuilder = null, string? route = null, ContentType contentType = ContentType.Json, ICqrsAuthorizer? authorizer = null)
        {
            this.applicationBuilder = applicationBuilder;
            this.settings = new KestrelCQRSServerLinkedSettings(route, authorizer, contentType);
            this.middlewareAdded = false;
        }

        public ICommandProducer? CreateCommandProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                throw new Exception($"{nameof(KestrelServiceCreator)}.{nameof(CreateCommandProducer)} requires {nameof(serviceUrl)}");
            return new KestrelCQRSClient(settings.ContentType, serviceUrl, symmetricConfig, settings.Authorizer);
        }

        public ICommandConsumer? CreateCommandConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateCommandConsumer)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<KestrelCQRSServerMiddleware>(symmetricConfig, settings);
            }
            return new KestrelCQRSServerCommandConsumer(settings);
        }

        public IEventProducer? CreateEventProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer? CreateEventConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient? CreateQueryClient(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                throw new Exception($"{nameof(KestrelServiceCreator)}.{nameof(CreateQueryClient)} requires {nameof(serviceUrl)}");
            return new KestrelCQRSClient(settings.ContentType, serviceUrl, symmetricConfig, settings.Authorizer);
        }

        public IQueryServer? CreateQueryServer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateQueryServer)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<KestrelCQRSServerMiddleware>(symmetricConfig, settings);
            }
            return new KestrelCQRSServerQueryServer(settings);
        }
    }
}
