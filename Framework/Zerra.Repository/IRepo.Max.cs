// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>Finds the largest <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TResult">The type of the value.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The largest value, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        TResult Max<TModel, TResult>(Expression<Func<TModel, TResult>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = Query(new Query<TModel>(QueryOperation.Max, where, null, value));
            if (result is null)
            {
                if (default(TResult) is not null)
                    throw new InvalidOperationException("Sequence contains no elements");
                return default!;
            }
            return (TResult)result;
        }

        /// <summary>Finds the largest <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TResult">The type of the value.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>The key and result of each group, in no particular order.</returns>
        IReadOnlyCollection<(TKey Key, TResult Value)> Max<TModel, TKey, TResult>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, TResult>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, TResult Value)>)Query(new CalculationGroupsQuery<TModel, TKey, TResult>(QueryOperation.Max, where, groupBy, value))!;
        }

        /// <summary>Asynchronously finds the largest <paramref name="value"/> over the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TResult">The type of the value.</typeparam>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The largest value, or null when no models match; throws when the result cannot be null, as LINQ does.</returns>
        async Task<TResult> MaxAsync<TModel, TResult>(Expression<Func<TModel, TResult>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            var result = await QueryAsync(new Query<TModel>(QueryOperation.Max, where, null, value));
            if (result is null)
            {
                if (default(TResult) is not null)
                    throw new InvalidOperationException("Sequence contains no elements");
                return default!;
            }
            return (TResult)result;
        }

        /// <summary>Asynchronously finds the largest <paramref name="value"/> per group of the models matching <paramref name="where"/>.</summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TResult">The type of the value.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="value">The value of each model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <returns>A task that represents the asynchronous operation. The key and result of each group, in no particular order.</returns>
        async Task<IReadOnlyCollection<(TKey Key, TResult Value)>> MaxAsync<TModel, TKey, TResult>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<TModel, TResult>> value, Expression<Func<TModel, bool>>? where = null) where TModel : class, new()
        {
            return (IReadOnlyCollection<(TKey Key, TResult Value)>)(await QueryAsync(new CalculationGroupsQuery<TModel, TKey, TResult>(QueryOperation.Max, where, groupBy, value)))!;
        }
    }
}
