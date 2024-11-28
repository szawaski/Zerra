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
    public sealed class TcpServiceCreator : IServiceCreator
    {
        private const ContentType contentType = ContentType.Bytes;
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCqrsServer> servers = new();

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new TcpRawCqrsClient(contentType, messageHost, symmetricConfig);
        }

        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, symmetricConfig, static (messageHost, symmetricConfig) => new TcpRawCqrsServer(contentType, messageHost, symmetricConfig));
        }

        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(TcpServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(TcpServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return new TcpRawCqrsClient(contentType, serviceUrl, symmetricConfig);
        }

        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return servers.GetOrAdd(serviceUrl, symmetricConfig, static (serviceUrl, symmetricConfig) => new TcpRawCqrsServer(contentType, serviceUrl, symmetricConfig));
        }
    }
}
