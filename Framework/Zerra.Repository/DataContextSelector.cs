// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public abstract class DataContextSelector : DataContext
    {
        protected override sealed IDataStoreEngine GetEngine()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                var contextName = context.GetType().Name;
                if (!context.TryGetEngine(out var engine))
                    continue;
                return engine;
            }
            return null;
        }

        private readonly object locker = new();
        private ICollection<DataContext>? contexts = null;
        protected ICollection<DataContext> GetDataContexts()
        {
            if (contexts is null)
            {
                lock (locker)
                {
                    contexts ??= LoadDataContexts();
                }
            }
            return contexts;
        }
        protected abstract ICollection<DataContext> LoadDataContexts();
    }
}
