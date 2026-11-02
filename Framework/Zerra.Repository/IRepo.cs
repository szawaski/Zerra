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

    }
}
