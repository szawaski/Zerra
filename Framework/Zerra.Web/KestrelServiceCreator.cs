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
        private readonly IHttpAuthorizer httpAuthorizer;
        private readonly CQRSServerMiddlewareSettings settings;
        private bool middlewareAdded;
        public KestrelServiceCreator(IApplicationBuilder applicationBuilder = null, string route = null, NetworkType networkType = NetworkType.Internal, ContentType contentType = ContentType.Json, IHttpAuthorizer httpAuthorizer = null)
        {
            this.applicationBuilder = applicationBuilder;
            this.httpAuthorizer = httpAuthorizer;
            this.settings = new CQRSServerMiddlewareSettings()
            {
                Route = route,
                HttpAuthorizer = httpAuthorizer,
                NetworkType = networkType,
                ContentType = contentType
            };
            this.middlewareAdded = false;
        }

        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return new KestrelCQRSClient(settings.NetworkType, settings.ContentType, serviceUrl, httpAuthorizer);
        }

        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            if (applicationBuilder == null)
                throw new NotSupportedException($"{nameof(KestrelServiceCreator)} needs {nameof(IApplicationBuilder)} for {nameof(CreateEventClient)}");

            if (!middlewareAdded)
            {
                middlewareAdded = true;
                _ = applicationBuilder.UseMiddleware<CQRSServerMiddleware>(settings);
            }
            return new CQRSServerMiddlewareCommandConsumer(settings);
        }

        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventClient)}");
        }

        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(KestrelServiceCreator)} does not support {nameof(CreateEventServer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return new KestrelCQRSClient(settings.NetworkType, settings.ContentType, serviceUrl, httpAuthorizer);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey)
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
