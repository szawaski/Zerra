// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    public interface IServiceCreator
    {
        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricConfig encryptionConfig);
        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricConfig encryptionConfig);
        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricConfig encryptionConfig);

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricConfig encryptionConfig);
        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricConfig encryptionConfig);
        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricConfig encryptionConfig);
    }
}