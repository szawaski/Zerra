// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that calculates a count, sum, average, minimum, maximum, or count of different values per group of the matching models.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TKey">The group key type, a member or an anonymous object of several.</typeparam>
    /// <typeparam name="TValue">The calculated value type.</typeparam>
    public sealed class CalculationGroupsQuery<TModel, TKey, TValue> : Query<TModel> where TModel : class, new()
    {
        /// <summary>
        /// Initializes a calculation query per group.
        /// </summary>
        /// <param name="operation">The calculation: <see cref="QueryOperation.Count"/>, <see cref="QueryOperation.Sum"/>, <see cref="QueryOperation.Average"/>, <see cref="QueryOperation.Min"/>, <see cref="QueryOperation.Max"/>, or <see cref="QueryOperation.CountDistinct"/>.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="groupBy">The group key expression.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        public CalculationGroupsQuery(QueryOperation operation, Expression<Func<TModel, bool>>? where, LambdaExpression groupBy, LambdaExpression? value)
            : base(operation, where, groupBy, value)
        {

        }

        /// <summary>
        /// Initializes a calculation query per group as a copy of an existing one, keeping the key and value types.
        /// </summary>
        /// <param name="query">The query to copy.</param>
        public CalculationGroupsQuery(Query query)
            : base(query)
        {

        }

        /// <inheritdoc/>
        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => engine.ExecuteCalculationGroups<TModel, TKey, TValue>(Operation, Where, GroupBy!, Value, modelDetail);
        /// <inheritdoc/>
        internal override async Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail) => await engine.ExecuteCalculationGroupsAsync<TModel, TKey, TValue>(Operation, Where, GroupBy!, Value, modelDetail);
        /// <inheritdoc/>
        internal override object? Execute(IEnumerable<object> models) => ((IEnumerable<TModel>)models).CalculateGroups<TModel, TKey, TValue>(Operation, GroupBy!, Value);
    }
}
