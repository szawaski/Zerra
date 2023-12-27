﻿// Copyright © KaKush LLC
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
        private static readonly ConcurrentFactoryDictionary<string, TcpRawCqrsServer> servers = new();

        public ICommandProducer? CreateCommandProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return TcpRawCqrsClient.CreateDefault(serviceUrl, symmetricConfig);
        }

        public ICommandConsumer? CreateCommandConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return servers.GetOrAdd(serviceUrl, (url) => TcpRawCqrsServer.CreateDefault(url, symmetricConfig));
        }

        public IEventProducer? CreateEventProducer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(TcpServiceCreator)} does not support {nameof(CreateEventProducer)}");
        }

        public IEventConsumer? CreateEventConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            throw new NotSupportedException($"{nameof(TcpServiceCreator)} does not support {nameof(CreateEventConsumer)}");
        }

        public IQueryClient? CreateQueryClient(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return TcpRawCqrsClient.CreateDefault(serviceUrl, symmetricConfig);
        }

        public IQueryServer? CreateQueryServer(string? serviceUrl, SymmetricConfig? symmetricConfig)
        {
            if (String.IsNullOrWhiteSpace(serviceUrl))
                return null;
            return servers.GetOrAdd(serviceUrl, (url) => TcpRawCqrsServer.CreateDefault(url, symmetricConfig));
        }
    }
}
