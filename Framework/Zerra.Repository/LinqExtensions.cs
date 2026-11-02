// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Extension methods for applying a <see cref="QueryOrder"/> and paging to LINQ sequences and queryables, and for calculating over and projecting sequences in memory.
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
        /// Applies the filtering, ordering, skip, and take to a sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IEnumerable{TSource}"/> that contains elements from the input sequence that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IEnumerable<TSource> Query<TSource>(this IEnumerable<TSource> source, LambdaExpression? where, QueryOrder? order, int? skip, int? take, bool caseSensitive = false)
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
        /// Applies the filtering, ordering, skip, and take to a queryable.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The queryable to query.</param>
        /// <param name="where">The filter expression to apply, or null for no filtering.</param>
        /// <param name="order">The <see cref="QueryOrder"/> that defines the ordering expressions, or null for no ordering.</param>
        /// <param name="skip">The number of elements to skip, or null to skip none.</param>
        /// <param name="take">The number of elements to take, or null to take all.</param>
        /// <param name="caseSensitive">Indicates if string comparisons should be case sensitive.</param>
        /// <returns>An <see cref="IQueryable{TSource}"/> that contains elements from the input queryable that satisfy the query.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="where"/> is not of type <c>Expression&lt;Func&lt;TSource, bool&gt;&gt;</c>.</exception>
        public static IQueryable<TSource> Query<TSource>(this IQueryable<TSource> source, LambdaExpression? where, QueryOrder? order, int? skip, int? take, bool caseSensitive = false)
            where TSource : class, new()
        {
            var whereQuery = source;
            if (where is not null)
            {
                var whereExpression = where as Expression<Func<TSource, bool>>;
                if (whereExpression == null)
                    throw new ArgumentException($"Where must be of type Expression<Func<TSource, bool>>");
                whereQuery = whereQuery.Where(MemoryExpressionRewriter.Rewrite(whereExpression, caseSensitive));
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
        /// Reads what an update by condition sets, <c>new TModel { Member = ..., ... }</c>, into its members.
        /// </summary>
        /// <param name="set">The members to set and their values in terms of the model.</param>
        /// <returns>The members in the order they are set.</returns>
        public static ProjectionMember[] GetProjectionMembers(this LambdaExpression set)
        {
            if (set.Body is not MemberInitExpression init)
                throw new NotSupportedException($"An update sets its members with new {set.ReturnType.Name} {{ Member = ..., ... }}");

            var resultDetail = TypeAnalyzer.GetTypeDetail(set.ReturnType);
            var parameter = set.Parameters[0];
            var members = new ProjectionMember[init.Bindings.Count];
            for (var i = 0; i < members.Length; i++)
            {
                if (init.Bindings[i] is not MemberAssignment assignment)
                    throw new NotSupportedException($"An update sets each member of {set.ReturnType.Name} directly");
                members[i] = new ProjectionMember(resultDetail.GetMember(assignment.Member.Name), assignment.Expression, parameter);
            }
            return members;
        }
    }
}
