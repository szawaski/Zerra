// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// A <see cref="DataContext"/> that selects the first available engine from a collection of candidate <see cref="DataContext"/> instances.
    /// </summary>
    public abstract class DataContextSelector : DataContext
    {
        /// <inheritdoc/>
        protected override sealed IDataStoreEngine? GetEngine()
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
        /// <summary>
        /// Returns the cached collection of candidate <see cref="DataContext"/> instances, initializing it on first access.
        /// </summary>
        /// <returns>The collection of <see cref="DataContext"/> instances to select from.</returns>
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
        /// <summary>
        /// Loads the collection of candidate <see cref="DataContext"/> instances used for engine selection.
        /// </summary>
        /// <returns>The collection of <see cref="DataContext"/> instances to select from.</returns>
        protected abstract ICollection<DataContext> LoadDataContexts();
    }
}
