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
        /// Executes a query and returns its result as the type the operation produces: the models, a model, a count, a calculation, or the typed results of a projection, calculation per group, or distinct values.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The result, boxed when it is a value.</returns>
        object? Query(Query query);

        /// <summary>
        /// Executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        void Persist(Persist persist);

        /// <summary>
        /// Asynchronously executes a query and returns its result as the type the operation produces: the models, a model, a count, a calculation, or the typed results of a projection, calculation per group, or distinct values.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result, boxed when it is a value.</returns>
        Task<object?> QueryAsync(Query query);

        /// <summary>
        /// Asynchronously executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PersistAsync(Persist persist);
    }
}
