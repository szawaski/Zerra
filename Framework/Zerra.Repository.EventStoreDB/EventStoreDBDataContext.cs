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

        protected override IDataStoreEngine GetEngine()
        {
            _ = Log.InfoAsync($"{nameof(EventStoreDBDataContext)} connecting to {ConnectionString}");
            var engine = new EventStoreDBEngine(ConnectionString, Insecure);
            return engine;
        }
    }
}
