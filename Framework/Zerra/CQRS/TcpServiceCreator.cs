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
    /// The fastest protocol for communication between services.
    /// This uses a light-weight TCP spec with fast and small <see cref="Zerra.Serialization.Bytes.ByteSerializer"/>
    /// Used in <see cref="Bus.StartServices(ServiceSettings, IServiceCreator)"/>.
    /// </summary>
    public sealed class TcpServiceCreator : IServiceCreator
    {
        private const ContentType contentType = ContentType.Bytes;
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCqrsServer> servers = new();

        /// <inheritdoc />
        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new TcpRawCqrsClient(contentType, messageHost, symmetricConfig);
        }

        /// <inheritdoc />
        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, symmetricConfig, static (messageHost, symmetricConfig) => new TcpRawCqrsServer(contentType, messageHost, symmetricConfig));
        }

        /// <inheritdoc />
        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return new TcpRawCqrsClient(contentType, messageHost, symmetricConfig);
        }

        /// <inheritdoc />
        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(messageHost))
                return null;
            return servers.GetOrAdd(messageHost, symmetricConfig, static (messageHost, symmetricConfig) => new TcpRawCqrsServer(contentType, messageHost, symmetricConfig));
        }

        /// <inheritdoc />
        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return new TcpRawCqrsClient(contentType, serviceUrl, symmetricConfig);
        }

        /// <inheritdoc />
        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return servers.GetOrAdd(serviceUrl, symmetricConfig, static (serviceUrl, symmetricConfig) => new TcpRawCqrsServer(contentType, serviceUrl, symmetricConfig));
        }
    }
}
