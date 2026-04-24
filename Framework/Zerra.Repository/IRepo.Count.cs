// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Gets the count of models matching the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The count of models matching the criteria.</returns>
        long Count<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)Query(new Query<TModel>(QueryOperation.Count, where, null, null, null, null))!;

        /// <summary>
        /// Asynchronously gets the count of models matching the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of models matching the criteria.</returns>
        async Task<long> CountAsync<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)(await QueryAsync(new Query<TModel>(QueryOperation.Count, where, null, null, null, null)))!;
    }
}
