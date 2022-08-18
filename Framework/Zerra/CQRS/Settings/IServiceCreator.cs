// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    public interface IServiceCreator
    {
        public ICommandConsumer CreateCommandConsumer(string serviceUrl, SymmetricKey encryptionKey);
        public IEventConsumer CreateEventConsumer(string serviceUrl, SymmetricKey encryptionKey);
        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey);

        public ICommandProducer CreateCommandProducer(string serviceUrl, SymmetricKey encryptionKey);
        public IEventProducer CreateEventProducer(string serviceUrl, SymmetricKey encryptionKey);
        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey);
    }
}