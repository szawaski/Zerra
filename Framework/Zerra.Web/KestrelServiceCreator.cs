// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using System;
using Zerra.Collections;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.Web
{
    public class KestrelServiceCreator : IServiceCreator
    {
        private static readonly ConcurrentFactoryDictionary<string, HttpCQRSServer> servers = new();

        private readonly IApplicationBuilder applicationBuilder;
        private readonly CQRSServerMiddlewareSettings settings;
        private bool middlewareAdded;
        public KestrelServiceCreator(IApplicationBuilder applicationBuilder = null, string route = null, ContentType contentType = ContentType.Json, ICQRSAuthorizer authorizer = null)
        {
            this.applicationBuilder = applicationBuilder;
            this.settings = new CQRSServerMiddlewareSettings()
            {
                Route = route,
                Authorizer = authorizer,
                ContentType = contentType
            };
            this.middlewareAdded = false;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return new KestrelCQRSClient(settings.ContentType, serviceUrl, symmetricConfig, settings.Authorizer);
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateEventProducer)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<CQRSServerMiddleware>(symmetricConfig, settings);
            }
            return new CQRSServerMiddlewareCommandConsumer(settings);
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            return new KestrelCQRSClient(settings.ContentType, serviceUrl, symmetricConfig, settings.Authorizer);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricConfig symmetricConfig)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateQueryServer)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<CQRSServerMiddleware>(symmetricConfig, settings);
            }
            return new CQRSServerMiddlewareQueryServer(settings);
        }
    }
}
