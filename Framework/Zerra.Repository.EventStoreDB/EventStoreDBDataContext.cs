// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.EventStoreDB
{
    public abstract class EventStoreDBDataContext : DataContext
    {
        public abstract string ConnectionString { get; }
        public abstract bool Insecure { get; }

        private readonly object locker = new();
        private IDataStoreEngine? engine = null;
        protected override IDataStoreEngine GetEngine()
        {
            if (engine == null)
            {
                lock (locker)
                {
                    if (engine == null)
                    {
                        var engine = new EventStoreDBEngine(ConnectionString, Insecure);
                        return engine;
                    }
                }
            }
            return engine;
        }
    }
}
