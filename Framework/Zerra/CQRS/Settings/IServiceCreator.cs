// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    public interface IServiceCreator
    {
        public ICommandConsumer? CreateCommandConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig);
        public IEventConsumer? CreateEventConsumer(string? serviceUrl, SymmetricConfig? symmetricConfig);
        public IQueryServer? CreateQueryServer(string? serviceUrl, SymmetricConfig? symmetricConfig);

        public ICommandProducer? CreateCommandProducer(string? serviceUrl, SymmetricConfig? symmetricConfig);
        public IEventProducer? CreateEventProducer(string? serviceUrl, SymmetricConfig? symmetricConfig);
        public IQueryClient? CreateQueryClient(string? serviceUrl, SymmetricConfig? symmetricConfig);
    }
}