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
        /// Starts a LINQ query of the model's store. Compose it with <c>Where</c>, <c>OrderBy</c>, <c>Skip</c>, <c>Take</c>, <c>Select</c>, <c>Distinct</c>, and <c>GroupBy</c>, and run it by enumerating it or with <c>First</c>, <c>Single</c>, <c>Count</c>, <c>Any</c>, <c>All</c>, <c>Sum</c>, <c>Min</c>, <c>Max</c>, or <c>Average</c>, or their <c>Async</c> versions from <see cref="RepoQueryableExtensions"/>.
        /// The query runs in the store, through the model's provider, as the other methods do.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="graph">The members and relations of the models to read, or <see langword="null"/> for every member and no relations. A query with a graph has no <c>Select</c> or <c>GroupBy</c>.</param>
        /// <returns>The queryable.</returns>
        IQueryable<TModel> Query<TModel>(Graph? graph = null) where TModel : class, new();

        /// <summary>
        /// Starts a LINQ query of the history of the model in its event store: each state the model was in, in the range given, for the models whose identity the query's <c>Where</c> names.
        /// Compose and run it as <see cref="Query{TModel}"/>; it runs over the states read, and <c>First</c> is the first in <paramref name="order"/>, so with the default it is the state as of <paramref name="dateTo"/> or <paramref name="numberTo"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">The order of the states, newest first by default.</param>
        /// <param name="dateFrom">The earliest date of the events to read.</param>
        /// <param name="dateTo">The latest date of the events to read.</param>
        /// <param name="numberFrom">The first event number to read.</param>
        /// <param name="numberTo">The last event number to read.</param>
        /// <param name="graph">The members and relations of the models to read, or <see langword="null"/> for every member and no relations.</param>
        /// <returns>The queryable.</returns>
        IQueryable<TModel> TemporalQuery<TModel>(TemporalOrder order = TemporalOrder.Newest, DateTime? dateFrom = null, DateTime? dateTo = null, ulong? numberFrom = null, ulong? numberTo = null, Graph? graph = null) where TModel : class, new();

        /// <summary>
        /// Starts a LINQ query of the events of the model in its event store: each event in the range given with the state it left the model in, for the models whose identity the query's <c>Where</c> names through <see cref="EventModel{TModel}.Model"/>.
        /// Compose and run it as <see cref="Query{TModel}"/>, with conditions and orders on the event too, such as <c>x =&gt; x.Model.ID == id &amp;&amp; x.EventName == "Shipped"</c>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="order">The order of the events, newest first by default.</param>
        /// <param name="dateFrom">The earliest date of the events to read.</param>
        /// <param name="dateTo">The latest date of the events to read.</param>
        /// <param name="numberFrom">The first event number to read.</param>
        /// <param name="numberTo">The last event number to read.</param>
        /// <param name="graph">The members and relations of the model of each event to read, or <see langword="null"/> for every member and no relations.</param>
        /// <returns>The queryable.</returns>
        IQueryable<EventModel<TModel>> EventQuery<TModel>(TemporalOrder order = TemporalOrder.Newest, DateTime? dateFrom = null, DateTime? dateTo = null, ulong? numberFrom = null, ulong? numberTo = null, Graph? graph = null) where TModel : class, new();

        /// <summary>
        /// Starts a unit of work: what the current flow of work reads and writes through the repository is in it until it commits or is disposed, which rolls it back.
        /// Each engine used joins it. On one engine it is all or nothing, across engines they commit one at a time. An event store can't take part.
        /// A method that is async must commit in the method that began it, as the flow it runs in ends when it returns.
        /// </summary>
        /// <param name="isolationLevel">The isolation level, or the engine's default.</param>
        /// <returns>The transaction, to commit and dispose.</returns>
        IRepoTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.Unspecified);
    }
}
