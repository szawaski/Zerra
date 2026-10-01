// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Memory
{
    /// <summary>
    /// Creates in-memory engines.
    /// </summary>
    public static class MemoryDataContext
    {
        /// <summary>
        /// Creates a new, empty in-memory store. It's both a transact store and an event store.
        /// </summary>
        /// <remarks>
        /// Each engine is its own store. Give the same engine to providers that share a store, the way providers share a database;
        /// providers for models related to each other need the same engine. Create another to start an empty store, such as one per test.
        /// </remarks>
        /// <returns>The engine to give the store providers and aggregates.</returns>
        public static MemoryEngine GetEngine() => new();
    }
}
