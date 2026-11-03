// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.Linq;

namespace Zerra.Repository
{
    /// <summary>
    /// Defines the contract for a transact store provider that can execute queries and persist operations against a data store.
    /// </summary>
    public interface ITransactStoreProvider
    {

        /// <summary>Executes a synchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        void Persist(Persist persist);
        /// <summary>Executes an asynchronous persist operation (create, update, or delete).</summary>
        /// <param name="persist">The persist operation to execute.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PersistAsync(Persist persist, CancellationToken cancellationToken);

        /// <summary>
        /// Runs a query and returns its elements as a list of its result type, or its value boxed. A <see cref="LinqModelQuery"/> gives what its ending call gives.
        /// </summary>
        /// <param name="query">The query, which a layer may add conditions to for the call.</param>
        /// <returns>The elements or the value.</returns>
        object? Query(LinqQuery query);

        /// <summary>
        /// Asynchronously runs a LINQ query from <see cref="IRepo.Query{TModel}"/> and returns its elements as a list of its result type, or its value boxed.
        /// </summary>
        /// <param name="query">The query, which a layer may add conditions to for the call.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>A task containing the elements or the value.</returns>
        Task<object?> QueryAsync(LinqQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Runs a LINQ query from <see cref="IRepo.Query{TModel}"/> ending in its elements and gives each one as it's read.
        /// </summary>
        /// <param name="query">The query, which a layer may add conditions to until its first element is read.</param>
        /// <returns>The elements, an <see cref="IEnumerable{T}"/> of the query's result type.</returns>
        object QueryStream(LinqQuery query);

        /// <summary>
        /// Runs a LINQ query from <see cref="IRepo.Query{TModel}"/> ending in its elements and gives each one as it's read.
        /// </summary>
        /// <param name="query">The query, which a layer may add conditions to until its first element is read.</param>
        /// <param name="cancellationToken">A token that cancels the query.</param>
        /// <returns>The elements, an <see cref="IAsyncEnumerable{T}"/> of the query's result type.</returns>
        object QueryStreamAsync(LinqQuery query, CancellationToken cancellationToken);

        //a query of this provider's model the provider of another makes, with the result type it has and this model's type only known here
        internal LinqQuery ReadElements<TResult>(System.Linq.Expressions.Expression expression, LinqElements elements, LinqTerminal? terminal, System.Linq.Expressions.LambdaExpression? argument);
    }
}
