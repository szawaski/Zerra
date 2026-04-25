// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a repository that supports querying and persisting data models.
    /// </summary>
    public partial interface IRepo
    {
        /// <summary>
        /// Executes a query and returns the result as an object.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The query result, or <c>null</c> if no result is found.</returns>
        object? Query(Query query);

        /// <summary>
        /// Executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        void Persist(Persist persist);

        /// <summary>
        /// Asynchronously executes a query and returns the result as an object.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the query result or <c>null</c>.</returns>
        Task<object?> QueryAsync(Query query);

        /// <summary>
        /// Asynchronously executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PersistAsync(Persist persist);
    }
}
