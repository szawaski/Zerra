// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    public interface IServiceCreator
    {
        public ICommandConsumer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey);
        public IEventConsumer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey);
        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey);
        public ICommandProducer CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey);
        public IEventProducer CreateEventClient(string serviceUrl, SymmetricKey encryptionKey);
        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey);
    }
}