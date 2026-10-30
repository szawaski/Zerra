// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public partial interface IRepo
    {
        /// <summary>
        /// Builds a result from each model matching <paramref name="where"/>, in the store, so only the members the projection sets are read.
        /// The projection is <c>x => new TResult { Member = ..., ... }</c>, each member taking what a <paramref name="where"/> takes, related models included.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TResult">The type of the result, with a settable member for each the projection sets.</typeparam>
        /// <param name="select">The projection building a result from a model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering of the models.</param>
        /// <param name="skip">The number of models to skip.</param>
        /// <param name="take">The number of models to take.</param>
        /// <returns>The results.</returns>
        IReadOnlyCollection<TResult> Select<TModel, TResult>(Expression<Func<TModel, TResult>> select, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null)
            where TModel : class, new()
            where TResult : class, new()
        {
            return (IReadOnlyCollection<TResult>)Query(new ProjectionQuery<TModel, TResult>(QueryOperation.Select, where, null, select, null, order, skip, take))!;
        }

        /// <summary>
        /// Asynchronously builds a result from each model matching <paramref name="where"/>, in the store, so only the members the projection sets are read.
        /// The projection is <c>x => new TResult { Member = ..., ... }</c>, each member taking what a <paramref name="where"/> takes, related models included.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TResult">The type of the result, with a settable member for each the projection sets.</typeparam>
        /// <param name="select">The projection building a result from a model.</param>
        /// <param name="where">An optional filter expression to apply to the query.</param>
        /// <param name="order">An optional ordering of the models.</param>
        /// <param name="skip">The number of models to skip.</param>
        /// <param name="take">The number of models to take.</param>
        /// <returns>A task that represents the asynchronous operation. The results.</returns>
        async Task<IReadOnlyCollection<TResult>> SelectAsync<TModel, TResult>(Expression<Func<TModel, TResult>> select, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null)
            where TModel : class, new()
            where TResult : class, new()
        {
            return (IReadOnlyCollection<TResult>)(await QueryAsync(new ProjectionQuery<TModel, TResult>(QueryOperation.Select, where, null, select, null, order, skip, take)))!;
        }

        /// <summary>
        /// Builds a result from each group of the models matching <paramref name="where"/>, in the store.
        /// The projection is <c>g => new TResult { Member = ..., ... }</c>, each member being the group's <c>Key</c>, a part of it such as <c>Key.A</c>, <c>Count()</c>, or <c>Sum</c>, <c>Average</c>, <c>Min</c>, or <c>Max</c> of a value.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <typeparam name="TResult">The type of the result, with a settable member for each the projection sets.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="select">The projection building a result from a group.</param>
        /// <param name="where">An optional filter expression to apply to the models.</param>
        /// <param name="having">An optional filter on the results.</param>
        /// <param name="order">An optional ordering of the results.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>The results, in no particular order unless <paramref name="order"/> is given.</returns>
        IReadOnlyCollection<TResult> Group<TModel, TKey, TResult>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<IGrouping<TKey, TModel>, TResult>> select, Expression<Func<TModel, bool>>? where = null, Expression<Func<TResult, bool>>? having = null, QueryOrder<TResult>? order = null, int? skip = null, int? take = null)
            where TModel : class, new()
            where TResult : class, new()
        {
            return (IReadOnlyCollection<TResult>)Query(new ProjectionQuery<TModel, TResult>(QueryOperation.Group, where, groupBy, select, having, order, skip, take))!;
        }

        /// <summary>
        /// Asynchronously builds a result from each group of the models matching <paramref name="where"/>, in the store.
        /// The projection is <c>g => new TResult { Member = ..., ... }</c>, each member being the group's <c>Key</c>, a part of it such as <c>Key.A</c>, <c>Count()</c>, or <c>Sum</c>, <c>Average</c>, <c>Min</c>, or <c>Max</c> of a value.
        /// </summary>
        /// <typeparam name="TModel">The type of the model to query.</typeparam>
        /// <typeparam name="TKey">The type of the group key, a member or an anonymous object of members such as <c>x => new { x.A, x.B }</c>.</typeparam>
        /// <typeparam name="TResult">The type of the result, with a settable member for each the projection sets.</typeparam>
        /// <param name="groupBy">The key the models are grouped by.</param>
        /// <param name="select">The projection building a result from a group.</param>
        /// <param name="where">An optional filter expression to apply to the models.</param>
        /// <param name="having">An optional filter on the results.</param>
        /// <param name="order">An optional ordering of the results.</param>
        /// <param name="skip">The number of results to skip.</param>
        /// <param name="take">The number of results to take.</param>
        /// <returns>A task that represents the asynchronous operation. The results, in no particular order unless <paramref name="order"/> is given.</returns>
        async Task<IReadOnlyCollection<TResult>> GroupAsync<TModel, TKey, TResult>(Expression<Func<TModel, TKey>> groupBy, Expression<Func<IGrouping<TKey, TModel>, TResult>> select, Expression<Func<TModel, bool>>? where = null, Expression<Func<TResult, bool>>? having = null, QueryOrder<TResult>? order = null, int? skip = null, int? take = null)
            where TModel : class, new()
            where TResult : class, new()
        {
            return (IReadOnlyCollection<TResult>)(await QueryAsync(new ProjectionQuery<TModel, TResult>(QueryOperation.Group, where, groupBy, select, having, order, skip, take)))!;
        }
    }
}
