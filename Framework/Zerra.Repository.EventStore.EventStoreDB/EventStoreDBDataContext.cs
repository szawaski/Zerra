// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.EventStore.EventStoreDB
{
    public abstract class EventStoreDBDataContext : EventStoreDataContext
    {
        public abstract string ConnectionString { get; }
        public abstract bool Insecure { get; }

        private static IEventStoreEngine provider;
        private static readonly object providerLock = new object();
        public sealed override IEventStoreEngine GetEngine()
        {
            if (provider == null)
            {
                lock (providerLock)
                {
                    if (provider == null)
                    {
                        provider = new EventStoreDBProvider(ConnectionString, Insecure);
                    }
                }
            }
            return provider;
        }
    }
}
