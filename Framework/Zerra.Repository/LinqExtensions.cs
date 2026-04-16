// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public static class LinqExtensions
    {
        public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, QueryOrder order)
            where TSource : class, new()
        {
            IOrderedEnumerable<TSource>? result = null;
            foreach (var item in order.OrderExpressions.Reverse())
                result = item.OrderBy(result ?? source);
            return result!;
        }
        public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, QueryOrder order)
            where TSource : class, new()
        {
            IOrderedQueryable<TSource>? result = null;
            foreach (var item in order.OrderExpressions.Reverse())
                result = item.OrderBy(result ?? source);
            return result!;
        }

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
    }
}
