// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    //one OrderBy, OrderByDescending, ThenBy, or ThenByDescending before the Select of a related collection in a projection
    internal sealed class ProjectionOrderExpression(LambdaExpression expression, bool descending) : OrderExpression
    {
        public override LambdaExpression Expression => expression;

        public override bool Descending => descending;

        public override IOrderedEnumerable<T> OrderBy<T>(IEnumerable<T> source)
        {
            var rewritten = MemoryExpressionRewriter.Rewrite(expression, false);
            var key = System.Linq.Expressions.Expression.Lambda<Func<T, object?>>(System.Linq.Expressions.Expression.Convert(rewritten.Body, typeof(object)), rewritten.Parameters).Compile();
            return descending ? source.OrderByDescending(key, Comparer<object?>.Default) : source.OrderBy(key, Comparer<object?>.Default);
        }

        public override IOrderedQueryable<T> OrderBy<T>(IQueryable<T> source) => throw new NotSupportedException("A projection's rows are ordered as they are read");
    }
}
