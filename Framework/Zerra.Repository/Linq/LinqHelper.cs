// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// Filters, orders, and pages sequences in memory.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class LinqHelper
    {
        /// <summary>
        /// Applies the filtering, ordering, skip, and take to a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The order, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> that contains elements from the input sequence that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IEnumerable<TSource> Query<TSource>(IEnumerable<TSource> source, LambdaExpression? where, LinqOrder[]? order, int? skip, int? take, bool caseSensitive = false)
           where TSource : class, new()
        {
            var whereQuery = source;
            if (where is not null)
            {
                var whereExpression = where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(MemoryExpressionRewriter.Rewrite(whereExpression, caseSensitive).Compile());
            }

            var orderQuery = whereQuery;
            if (order is not null)
            {
                IOrderedEnumerable<TSource>? ordered = null;
                foreach (var orderBy in order)
                {
                    var lambda = MemoryExpressionRewriter.Rewrite(orderBy.Expression, caseSensitive);
                    var key = Expression.Lambda<Func<TSource, object?>>(Expression.Convert(lambda.Body, typeof(object)), lambda.Parameters).Compile();
                    if (ordered is null)
                        ordered = orderBy.Descending ? orderQuery.OrderByDescending(key, Comparer<object?>.Default) : orderQuery.OrderBy(key, Comparer<object?>.Default);
                    else
                        ordered = orderBy.Descending ? ordered.ThenByDescending(key, Comparer<object?>.Default) : ordered.ThenBy(key, Comparer<object?>.Default);
                }
                orderQuery = ordered ?? orderQuery;
            }

            var skiptake = orderQuery;
            if (skip.HasValue)
                skiptake = skiptake.Skip(skip.Value);
            if (take.HasValue)
                skiptake = skiptake.Take(take.Value);

            return skiptake;
        }
    }
}
