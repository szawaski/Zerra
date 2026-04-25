// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the contract for a transact store provider that can execute queries and persist operations against a data store.
    /// </summary>
    public interface ITransactStoreProvider
    {
        /// <summary>Executes a synchronous query operation and returns the result.</summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The query result, or <see langword="null"/> if no result is found.</returns>
        object? Query(Query query);
        /// <summary>Executes an asynchronous query operation and returns the result.</summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task containing the query result, or <see langword="null"/>.</returns>
        Task<object?> QueryAsync(Query query);

        /// <summary>Executes a synchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        void Persist(Persist persist);
        /// <summary>Executes an asynchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PersistAsync(Persist persist);
    }
}
