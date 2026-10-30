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
        /// <returns>The count of models matching the criteria.</returns>
        long Count<TModel>() where TModel : class, new()
            => (long)Query(new Query<TModel>(QueryOperation.Count, null, null, null, null, null))!;

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
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of models matching the criteria.</returns>
        async Task<long> CountAsync<TModel>() where TModel : class, new()
            => (long)(await QueryAsync(new Query<TModel>(QueryOperation.Count, null, null, null, null, null)))!;

        /// <summary>
        /// Asynchronously gets the count of models matching the specified filter criteria.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the count of models matching the criteria.</returns>
        async Task<long> CountAsync<TModel>(Expression<Func<TModel, bool>>? where) where TModel : class, new()
            => (long)(await QueryAsync(new Query<TModel>(QueryOperation.Count, where, null, null, null, null)))!;

        /// <summary>Counts the models matching <paramref name="where"/> per group.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and count of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, long Value)> Count<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, long Value)>)Query(new CalculationGroupsQuery<TModel, TKey, long>(QueryOperation.Count, where, groupBy, null))!;
        }

        /// <summary>Asynchronously counts the models matching <paramref name="where"/> per group.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and count of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, long Value)>> CountAsync<TModel, TKey>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, long Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, long>(QueryOperation.Count, where, groupBy, null)))!;
        }
    }
}
