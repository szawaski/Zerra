// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Returns the different values of <paramref name="value"/> over the models matching <paramref name="where"/>, in the store. A null is one of them when it occurs.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TValue">The type of the value, a member or an anonymous object of several.</typeparam>
        /// <param name="value">The value, taking what a <paramref name="where"/> takes.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The different values, in no particular order.</returns>
        IReadOnlyCollection<TValue> Distinct<TModel, TValue>(Expression<Func<TModel, TValue>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<object?>)Query(new Query<TModel>(QueryOperation.Distinct, where, value, null))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TValue>(value);
            var values = new TValue[rows.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = buildKey(rows[i]);
            return values;
        }

        /// <summary>
        /// Asynchronously returns the different values of <paramref name="value"/> over the models matching <paramref name="where"/>, in the store. A null is one of them when it occurs.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TValue">The type of the value, a member or an anonymous object of several.</typeparam>
        /// <param name="value">The value, taking what a <paramref name="where"/> takes.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The different values, in no particular order.</returns>
        async Task<IReadOnlyCollection<TValue>> DistinctAsync<TModel, TValue>(Expression<Func<TModel, TValue>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var rows = (IReadOnlyList<object?>)(await QueryAsync(new Query<TModel>(QueryOperation.Distinct, where, value, null)))!;
            var buildKey = CalculationCommon.GetKeyBuilder<TValue>(value);
            var values = new TValue[rows.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = buildKey(rows[i]);
            return values;
        }
    }
}
