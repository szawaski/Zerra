// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Determines whether any models match the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>True if any models match the criteria; otherwise, false.</returns>
        bool QueryAny<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (bool)Query(new Query<TModel>(QueryOperation.Any, where, null, null, null, null))!;

        /// <summary>
        /// Asynchronously determines whether any models match the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains true if any models match the criteria; otherwise, false.</returns>
        async Task<bool> QueryAnyAsync<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (bool)(await QueryAsync(new Query<TModel>(QueryOperation.Any, where, null, null, null, null)))!;
    }
}
