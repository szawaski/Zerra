// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;
using Zerra.Repository.EventStore;

namespace Zerra.Repository.EventStoreDB
{
    public abstract class EventStoreDBDataContext : DataContext<IEventStoreEngine>
    {
        public abstract string ConnectionString { get; }
        public abstract bool Insecure { get; }

        protected override sealed IEventStoreEngine GetEngine()
        {
            _ = Log.InfoAsync($"{nameof(EventStoreDBDataContext)} connecting to {ConnectionString}");
            var engine = new EventStoreDBEngine(ConnectionString, Insecure);
            return engine;
        }
    }
}
