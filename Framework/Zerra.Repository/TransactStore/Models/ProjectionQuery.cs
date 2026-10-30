// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that builds a <typeparamref name="TResult"/> from each matching model, or from each group of them, with a projection.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TResult">The projection's result type, a class with a settable member for each the projection sets.</typeparam>
    public sealed class ProjectionQuery<TModel, TResult> : Query<TModel> where TModel : class, new()
    {
        /// <summary>
        /// Initializes a projection query, of each model or of each group.
        /// </summary>
        /// <param name="operation"><see cref="QueryOperation.Select"/> or <see cref="QueryOperation.Group"/>.</param>
        /// <param name="where">An optional filter expression on the models.</param>
        /// <param name="groupBy">The group key expression for <see cref="QueryOperation.Group"/>, otherwise <see langword="null"/>.</param>
        /// <param name="select">The projection building a result from each model or group.</param>
        /// <param name="having">An optional filter on the results of a grouped query.</param>
        /// <param name="order">An optional sort order, on the models for a select and on the results for a group.</param>
        /// <param name="skip">The number of models or results to skip.</param>
        /// <param name="take">The maximum number of models or results to return.</param>
        public ProjectionQuery(QueryOperation operation, Expression<Func<TModel, bool>>? where, LambdaExpression? groupBy, LambdaExpression select, LambdaExpression? having, QueryOrder? order, int? skip, int? take)
            : base(operation, where, groupBy, select, having, order, skip, take)
        {

        }

        /// <summary>
        /// Initializes a projection query as a copy of an existing one, keeping the result type.
        /// </summary>
        /// <param name="query">The query to copy.</param>
        public ProjectionQuery(Query query)
            : base(query)
        {

        }

        /// <inheritdoc/>
        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail)
        {
            if (Operation == QueryOperation.Group)
                return engine.ExecuteGroup<TModel, TResult>(GroupBy!, Select!, Where, Having, Order, Skip, Take, modelDetail);
            return engine.ExecuteSelect<TModel, TResult>(Select!, Where, Order, Skip, Take, modelDetail);
        }
        /// <inheritdoc/>
        internal override async Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail)
        {
            if (Operation == QueryOperation.Group)
                return await engine.ExecuteGroupAsync<TModel, TResult>(GroupBy!, Select!, Where, Having, Order, Skip, Take, modelDetail);
            return await engine.ExecuteSelectAsync<TModel, TResult>(Select!, Where, Order, Skip, Take, modelDetail);
        }
        /// <inheritdoc/>
        internal override object? Execute(IEnumerable<object> models)
        {
            if (Operation == QueryOperation.Group)
                return ((IEnumerable<TModel>)models).Group<TModel, TResult>(GroupBy!, Select!, Having, Order, Skip, Take);
            return ((IEnumerable<TModel>)models).Select<TModel, TResult>(Select!);
        }
    }
}
