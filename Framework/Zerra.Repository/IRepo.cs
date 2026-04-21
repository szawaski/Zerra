// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a repository that supports querying and persisting data models.
    /// </summary>
    public interface IRepo
    {
        /// <summary>
        /// Executes a query and returns the result as an object.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The query result, or <c>null</c> if no result is found.</returns>
        object? Query(Query query);

        /// <summary>
        /// Executes a query that returns multiple models.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A read-only collection of matching models.</returns>
        IReadOnlyCollection<TModel> Query<TModel>(QueryMany<TModel> query) where TModel : class, new();

        /// <summary>
        /// Executes a query that returns the first matching model.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>The first matching model, or <c>null</c> if no result is found.</returns>
        TModel? Query<TModel>(QueryFirst<TModel> query) where TModel : class, new();

        /// <summary>
        /// Executes a query that returns a single matching model.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>The single matching model, or <c>null</c> if no result is found.</returns>
        TModel? Query<TModel>(QuerySingle<TModel> query) where TModel : class, new();

        /// <summary>
        /// Executes a query that checks whether any matching models exist.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns><c>true</c> if any matching models exist; otherwise, <c>false</c>.</returns>
        bool Query<TModel>(QueryAny<TModel> query) where TModel : class, new();

        /// <summary>
        /// Executes a query that returns the count of matching models.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>The number of matching models.</returns>
        long Query<TModel>(QueryCount<TModel> query) where TModel : class, new();

        /// <summary>
        /// Executes a query that returns multiple event models.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A read-only collection of matching event models.</returns>
        IReadOnlyCollection<EventModel<TModel>> Query<TModel>(EventQueryMany<TModel> query) where TModel : class, new();

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
        /// Asynchronously executes a query that returns multiple models.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing a read-only collection of matching models.</returns>
        Task<IReadOnlyCollection<TModel>> QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class, new();

        /// <summary>
        /// Asynchronously executes a query that returns the first matching model.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the first matching model or <c>null</c>.</returns>
        Task<TModel?> QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class, new();

        /// <summary>
        /// Asynchronously executes a query that returns a single matching model.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the single matching model or <c>null</c>.</returns>
        Task<TModel?> QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class, new();

        /// <summary>
        /// Asynchronously executes a query that checks whether any matching models exist.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing <c>true</c> if any matching models exist; otherwise, <c>false</c>.</returns>
        Task<bool> QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class, new();

        /// <summary>
        /// Asynchronously executes a query that returns the count of matching models.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing the number of matching models.</returns>
        Task<long> QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class, new();

        /// <summary>
        /// Asynchronously executes a query that returns multiple event models.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <returns>A task that represents the asynchronous operation, containing a read-only collection of matching event models.</returns>
        Task<IReadOnlyCollection<EventModel<TModel>>> QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class, new();

        /// <summary>
        /// Asynchronously executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PersistAsync(Persist persist);
    }
}
