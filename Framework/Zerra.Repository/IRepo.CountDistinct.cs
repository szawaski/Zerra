// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Counts the different values of <paramref name="value"/> over the models matching <paramref name="where"/>, in the store. Nulls aren't counted.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value whose different occurrences are counted, taking what a <paramref name="where"/> takes.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The number of different values.</returns>
        long CountDistinct<TModel, TValue>(Expression<Func<TModel, TValue>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (long)Query(new Query<TModel>(QueryOperation.CountDistinct, where, null, value))!;
        }

        /// <summary>
        /// Counts the different values of <paramref name="value"/> per group of the models matching <paramref name="where"/>, in the store. Nulls aren't counted.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="groupBy">The group key, a member or an anonymous object of several.</param>
        /// <param name="value">The value whose different occurrences are counted, taking what a <paramref name="where"/> takes.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and the number of different values of each group.</returns>
        IReadOnlyCollection<(TKey Key, long Value)> CountDistinct<TModel, TKey, TValue>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, TValue>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)Query(new Query<TModel>(QueryOperation.CountDistinct, where, groupBy, value))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, long Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), (long)result!);
            }
            return groups;
        }

        /// <summary>
        /// Asynchronously counts the different values of <paramref name="value"/> over the models matching <paramref name="where"/>, in the store. Nulls aren't counted.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="value">The value whose different occurrences are counted, taking what a <paramref name="where"/> takes.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The number of different values.</returns>
        async Task<long> CountDistinctAsync<TModel, TValue>(Expression<Func<TModel, TValue>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (long)(await QueryAsync(new Query<TModel>(QueryOperation.CountDistinct, where, null, value)))!;
        }

        /// <summary>
        /// Asynchronously counts the different values of <paramref name="value"/> per group of the models matching <paramref name="where"/>, in the store. Nulls aren't counted.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="groupBy">The group key, a member or an anonymous object of several.</param>
        /// <param name="value">The value whose different occurrences are counted, taking what a <paramref name="where"/> takes.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and the number of different values of each group.</returns>
        async Task<IReadOnlyCollection<(TKey Key, long Value)>> CountDistinctAsync<TModel, TKey, TValue>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, TValue>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<(object? Key, object? Value)>)(await QueryAsync(new Query<TModel>(QueryOperation.CountDistinct, where, groupBy, value)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TKey>(groupBy);
            var groups = new (TKey Key, long Value)[rows.Count];
            for (var i = 0; i < groups.Length; i++)
            {
                var (key, result) = rows[i];
                groups[i] = (buildKey(key), (long)result!);
            }
            return groups;
        }
    }
}
