// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public static class LinqExtensions
    {
        private static readonly MethodInfo queryableOrderBy = typeof(Queryable).GetMethods().First(m => m.Name == "OrderBy" && m.GetParameters().Length == 2);
        private static readonly MethodInfo queryableOrderByDescending = typeof(Queryable).GetMethods().First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2);
        //public static IOrderedQueryable<TSource> OrderBy<TSource, TModel>(this IQueryable<TSource> source, QueryOrder<TModel> linqOrder)
        //    where TModel : class, new()
        //{
        //    IQueryable<TSource> result = source;
        //    foreach (var order in linqOrder.OrderExpressions.AsEnumerable().Reverse())
        //    {
        //        var sourceOrder = LinqRebinder.RebindType(order.Expression, typeof(TModel), typeof(TSource));
        //        var propertyType = ((LambdaExpression)sourceOrder).ReturnType;
        //        if (order.Descending)
        //        {
        //            var method = TypeAnalyzer.GetGenericMethod(queryableOrderByDescending, typeof(TSource), propertyType);
        //            result = (IOrderedQueryable<TSource>)method.Caller(null, new object[] { result, sourceOrder });
        //        }
        //        else
        //        {
        //            var method = TypeAnalyzer.GetGenericMethod(queryableOrderBy, typeof(TSource), propertyType);
        //            result = (IOrderedQueryable<TSource>)method.Caller(null, new object[] { result, sourceOrder });
        //        }
        //    }
        //    return (IOrderedQueryable<TSource>)result;
        //}
        public static IOrderedQueryable<TModel> OrderBy<TModel>(this IQueryable<TModel> source, QueryOrder<TModel> linqOrder)
            where TModel : class, new()
        {
            var result = source;
            foreach (var order in linqOrder.OrderExpressions.AsEnumerable().Reverse())
            {
                var propertyType = ((LambdaExpression)order.Expression).ReturnType;
                if (order.Descending)
                {
                    var method = GenericTypeCache.GetGenericMethodDetail(queryableOrderByDescending, typeof(TModel), propertyType);
                    result = (IOrderedQueryable<TModel>)method.CallerBoxed(null, [result, order.Expression])!;
                }
                else
                {
                    var method = GenericTypeCache.GetGenericMethodDetail(queryableOrderBy, typeof(TModel), propertyType);
                    result = (IOrderedQueryable<TModel>)method.CallerBoxed(null, [result, order.Expression])!;
                }
            }
            return (IOrderedQueryable<TModel>)result;
        }

        //public static IQueryable<TSource> QueryChangeTypes<TSource, TModel>(this IQueryable<TSource> source, Query<TModel> query)
        //     where TModel : class, new()
        //{
        //    IQueryable<TSource> whereQuery = source;
        //    if (query.Where is not null)
        //    {
        //        var sourceWhere = (Expression<Func<TSource, bool>>)LinqRebinder.RebindType(query.Where, typeof(TModel), typeof(TSource));
        //        whereQuery = whereQuery.Where(sourceWhere);
        //    }

        //    IQueryable<TSource> orderQuery = whereQuery;
        //    if (query.Order is not null)
        //    {
        //        orderQuery = orderQuery.OrderBy(query.Order);
        //    }

        //    IQueryable<TSource> skiptake = orderQuery;
        //    if (query.Skip.HasValue)
        //        skiptake = skiptake.Skip(query.Skip.Value);
        //    if (query.Take.HasValue)
        //        skiptake = skiptake.Take(query.Take.Value);

        //    return skiptake;
        //}

        public static IQueryable<TModel> Query<TModel>(this IQueryable<TModel> source, Query<TModel> query)
            where TModel : class, new()
        {
            var whereQuery = source;
            if (query.Where is not null)
            {
                whereQuery = whereQuery.Where(query.Where);
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
