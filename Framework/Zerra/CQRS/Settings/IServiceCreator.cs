// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    public interface IServiceCreator
    {
        public ICommandConsumer? CreateCommandConsumer(string messageHost, SymmetricConfig? symmetricConfig);
        public IEventConsumer? CreateEventConsumer(string messageHost, SymmetricConfig? symmetricConfig);
        public IQueryServer? CreateQueryServer(string serviceUrl, SymmetricConfig? symmetricConfig);

        public ICommandProducer? CreateCommandProducer(string messageHost, SymmetricConfig? symmetricConfig);
        public IEventProducer? CreateEventProducer(string messageHost, SymmetricConfig? symmetricConfig);
        public IQueryClient? CreateQueryClient(string serviceUrl, SymmetricConfig? symmetricConfig);
    }
}