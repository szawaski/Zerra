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
        public KestrelServiceCreator(IApplicationBuilder applicationBuilder = null, string route = null, NetworkType networkType = NetworkType.Internal, ContentType contentType = ContentType.Json, IHttpAuthorizer httpAuthorizer = null)
        {
            this.applicationBuilder = applicationBuilder;
            this.settings = new CQRSServerMiddlewareSettings()
            {
                Route = route,
                HttpAuthorizer = httpAuthorizer,
                NetworkType = networkType,
                ContentType = contentType
            };
            this.middlewareAdded = false;
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig encryptionConfig)
        {
            return new KestrelCQRSClient(settings.NetworkType, settings.ContentType, serviceUrl, settings.HttpAuthorizer);
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig encryptionConfig)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateEventProducer)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<CQRSServerMiddleware>(settings);
            }
            return new CQRSServerMiddlewareCommandConsumer(settings);
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig encryptionConfig)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig encryptionConfig)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricConfig encryptionConfig)
        {
            return new KestrelCQRSClient(settings.NetworkType, settings.ContentType, serviceUrl, settings.HttpAuthorizer);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricConfig encryptionConfig)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateQueryServer)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<CQRSServerMiddleware>(settings);
            }
            return new CQRSServerMiddlewareQueryServer(settings);
        }
    }
}
