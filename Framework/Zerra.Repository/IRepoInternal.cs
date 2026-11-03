// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    /// <summary>
    /// Internal extension of <see cref="IRepo"/> that exposes provider resolution for cross-repo operations and running a query or persist as a provider makes it.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface IRepoInternal : IRepo
    {
        /// <summary>
        /// Executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        void Persist(Persist persist);

        /// <summary>
        /// Asynchronously executes a persist operation to create, update, or delete data.
        /// </summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task PersistAsync(Persist persist, CancellationToken cancellationToken);

        /// <summary>
        /// Runs a query and returns its elements as a list of its result type, or its value boxed. A <see cref="LinqModelQuery"/> gives models or event models.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The result, boxed when it is a value.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object? Query(LinqQuery query);

        /// <summary>
        /// Asynchronously runs a query and returns its elements as a list of its result type, or its value boxed. A <see cref="LinqModelQuery"/> gives models or event models.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result, boxed when it is a value.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task<object?> QueryAsync(LinqQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Runs a query ending in its elements and gives each one as it's read.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>The elements, an <see cref="IEnumerable{T}"/> of the query's result type.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object QueryStream(LinqQuery query);

        /// <summary>
        /// Runs a query ending in its elements and gives each one as it's read.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>The elements, an <see cref="IAsyncEnumerable{T}"/> of the query's result type.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        object QueryStreamAsync(LinqQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Attempts to retrieve the <see cref="ITransactStoreProvider"/> registered for the specified model type.
        /// </summary>
        /// <param name="modelType">The model type to look up.</param>
        /// <param name="provider">When this method returns <see langword="true"/>, contains the matching <see cref="ITransactStoreProvider"/>; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a provider was found for <paramref name="modelType"/>; otherwise, <see langword="false"/>.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        bool TryGetProvider(Type modelType, [MaybeNullWhen(false)] out ITransactStoreProvider provider);
    }
}