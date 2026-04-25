// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Extension methods for applying <see cref="QueryOrder"/> and <see cref="Zerra.Repository.Query"/> to LINQ sequences and queryables.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Sorts the elements of a sequence according to the specified <see cref="QueryOrder"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TSource}"/> whose elements are sorted according to <paramref name="order"/>.</returns>
        public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, QueryOrder order)
            where TSource : class, new()
        {
            IOrderedEnumerable<TSource>? result = null;
            foreach (var item in order.OrderExpressions.Reverse())
                result = item.OrderBy(result ?? source);
            return result!;
        }

        /// <summary>
        /// Sorts the elements of a queryable according to the specified <see cref="QueryOrder"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to order.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions.</param>
        /// <returns>An <see cref="IOrderedQueryable{TSource}"/> whose elements are sorted according to <paramref name="order"/>.</returns>
        public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, QueryOrder order)
            where TSource : class, new()
        {
            IOrderedQueryable<TSource>? result = null;
            foreach (var item in order.OrderExpressions.Reverse())
                result = item.OrderBy(result ?? source);
            return result!;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take from a <see cref="Zerra.Repository.Query"/> to a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <param name="query">The <see cref="Zerra.Repository.Query"/> containing the where, order, skip, and take clauses.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> that contains elements from the input sequence that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="Query.Where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IEnumerable<TSource> Query<TSource>(this IEnumerable<TSource> source, Query query)
           where TSource : class, new()
        {
            var whereQuery = source;
            if (query.Where is not null)
            {
                var whereExpression = query.Where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(whereExpression.Compile());
            }

            var orderQuery = whereQuery;
            if (query.Order is not null)
            {
                orderQuery = orderQuery.OrderBy(query.Order);
            }

            var skiptake = orderQuery;
            if (query.Skip.HasValue)
                skiptake = skiptake.Skip(query.Skip.Value);
            if (query.Take.HasValue)
                skiptake = skiptake.Take(query.Take.Value);

            return skiptake;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take to a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> that contains elements from the input sequence that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IEnumerable<TSource> Query<TSource>(this IEnumerable<TSource> source, Expression? where, QueryOrder? order, int? skip, int? take)
           where TSource : class, new()
        {
            var whereQuery = source;
            if (where is not null)
            {
                var whereExpression = where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(whereExpression.Compile());
            }

            var orderQuery = whereQuery;
            if (order is not null)
            {
                orderQuery = orderQuery.OrderBy(order);
            }

            var skiptake = orderQuery;
            if (skip.HasValue)
                skiptake = skiptake.Skip(skip.Value);
            if (take.HasValue)
                skiptake = skiptake.Take(take.Value);

            return skiptake;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take from a <see cref="Zerra.Repository.Query"/> to a queryable.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to query.</param>
        /// <param name="query">The <see cref="Zerra.Repository.Query"/> containing the where, order, skip, and take clauses.</param>
        /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input queryable that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <see cref="Query.Where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IQueryable<TSource> Query<TSource>(this IQueryable<TSource> source, Query query)
            where TSource : class, new()
        {
            var whereQuery = source;
            if (query.Where is not null)
            {
                var whereExpression = query.Where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(whereExpression);
            }

            var orderQuery = whereQuery;
            if (query.Order is not null)
            {
                orderQuery = orderQuery.OrderBy(query.Order);
            }

            var skiptake = orderQuery;
            if (query.Skip.HasValue)
                skiptake = skiptake.Skip(query.Skip.Value);
            if (query.Take.HasValue)
                skiptake = skiptake.Take(query.Take.Value);

            return skiptake;
        }

        /// <summary>
        /// Applies the filtering, ordering, skip, and take to a queryable.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input queryable that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IQueryable<TSource> Query<TSource>(this IQueryable<TSource> source, Expression? where, QueryOrder? order, int? skip, int? take)
            where TSource : class, new()
        {
            var whereQuery = source;
            if (where is not null)
            {
                var whereExpression = where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(whereExpression);
            }

            var orderQuery = whereQuery;
            if (order is not null)
            {
                orderQuery = orderQuery.OrderBy(order);
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
