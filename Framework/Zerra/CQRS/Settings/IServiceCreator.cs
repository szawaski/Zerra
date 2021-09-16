// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Encryption;

namespace Zerra.CQRS.Settings
{
    public interface IServiceCreator
    {
        public ICommandServer CreateCommandServer(string serviceUrl, SymmetricKey encryptionKey);
        public IEventServer CreateEventServer(string serviceUrl, SymmetricKey encryptionKey);
        public IQueryServer CreateQueryServer(string serviceUrl, SymmetricKey encryptionKey);
        public ICommandClient CreateCommandClient(string serviceUrl, SymmetricKey encryptionKey);
        public IEventClient CreateEventClient(string serviceUrl, SymmetricKey encryptionKey);
        public IQueryClient CreateQueryClient(string serviceUrl, SymmetricKey encryptionKey);
    }
}