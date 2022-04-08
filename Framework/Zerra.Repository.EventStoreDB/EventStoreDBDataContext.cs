// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;

namespace Zerra.Repository.EventStoreDB
{
    public abstract class EventStoreDBDataContext : DataContext
    {
        public abstract string ConnectionString { get; }
        public abstract bool Insecure { get; }

        private IDataStoreEngine engine = null;
        protected override IDataStoreEngine GetEngine()
        {
            if (engine == null)
            {
                lock (this)
                {
                    if (engine == null)
                    {
                        _ = Log.InfoAsync($"{nameof(EventStoreDBDataContext)} connecting to {ConnectionString}");
                        var engine = new EventStoreDBEngine(ConnectionString, Insecure);
                        return engine;
                    }
                }
            }
            return engine;
        }
    }
}
