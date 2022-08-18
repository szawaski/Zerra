// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Microsoft.AspNetCore.Builder;
using System;
using Zerra.CQRS;
using Zerra.CQRS.Settings;
using Zerra.Encryption;

namespace Zerra.Web
{
    public class GatewayServiceCreator : IServiceCreator
    {
        private readonly IServiceCreator serviceCreatorForClients;
        public GatewayServiceCreator(IApplicationBuilder applicationBuilder, IServiceCreator serviceCreatorForClients, string route = "/CQRS")
        {
            this.serviceCreatorForClients = serviceCreatorForClients;
            _ = applicationBuilder.UseMiddleware<CQRSGatewayMiddleware>(route);
        }

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return serviceCreatorForClients.CreateCommandProducer(serviceUrl, encryptionKey);
        }

        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(GatewayServiceCreator)} does not support {nameof(CreateCommandConsumer)}");
        }

        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricKey encryptionKey)
        {
            return serviceCreatorForClients.CreateEventProducer(serviceUrl, encryptionKey);
        }

        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(GatewayServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey)
        {
            return serviceCreatorForClients.CreateQueryClient(serviceUrl, encryptionKey);
        }

        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey)
        {
            throw new NotSupportedException($"{nameof(GatewayServiceCreator)} does not support {nameof(CreateQueryServer)}");
        }
    }
}
