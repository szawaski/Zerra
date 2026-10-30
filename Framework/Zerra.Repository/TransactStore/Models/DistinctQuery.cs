// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that returns the different values of an expression over the matching models.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TValue">The value type, a member or an anonymous object of several.</typeparam>
    public sealed class DistinctQuery<TModel, TValue> : Query<TModel> where TModel : class, new()
    {
        /// <summary>
        /// Initializes a query for the different values of an expression.
        /// </summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="value">The value expression, kept as the query's <see cref="Query.GroupBy"/>.</param>
        public DistinctQuery(Expression<Func<TModel, bool>>? where, LambdaExpression value)
            : base(QueryOperation.Distinct, where, value, null)
        {

        }

        /// <summary>
        /// Initializes a query for the different values as a copy of an existing one, keeping the value type.
        /// </summary>
        /// <param name="query">The query to copy.</param>
        public DistinctQuery(Query query)
            : base(query)
        {

        }

        /// <inheritdoc/>
        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => engine.ExecuteDistinct<TModel, TValue>(GroupBy!, Where, modelDetail);
        /// <inheritdoc/>
        internal override async Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail) => await engine.ExecuteDistinctAsync<TModel, TValue>(GroupBy!, Where, modelDetail);
        /// <inheritdoc/>
        internal override object? Execute(IEnumerable<object> models) => ((IEnumerable<TModel>)models).Distinct<TModel, TValue>(GroupBy!);
    }
}
