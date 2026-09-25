// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Memory
{
    /// <summary>
    /// Abstract base class for a Memory data context.
    /// </summary>
    /// <remarks>
    /// Each context instance creates its own <see cref="MemoryEngine"/>, and each engine is its own store. Store providers created without a
    /// context share one instance per context type, so they share one store, the way providers share a database. Give providers a new
    /// instance to start an empty store of their own, such as one per test; providers for models related to each other need the same instance.
    /// </remarks>
    public class MemoryDataContext : DataContext
    {
        private readonly Lock locker = new();
        private IDataStoreEngine? engine = null;
        /// <inheritdoc/>
        protected override sealed IDataStoreEngine GetEngine()
        {
            if (engine is null)
            {
                lock (locker)
                {
                    engine ??= new MemoryEngine();
                }
            }
            return engine;
        }
    }
}
