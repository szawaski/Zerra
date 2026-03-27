// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    public sealed class OrderExpression<TSource, TKey> : OrderExpression
        where TSource : class, new()
    {
        private readonly Expression<Func<TSource, TKey>> expression;
        private readonly bool descending;

        public override Expression Expression => expression;
        public override bool Descending => descending;

        public OrderExpression(Expression<Func<TSource, TKey>> expression, bool descending)
        {
            this.expression = expression;
            this.descending = descending;
        }

        public override sealed IOrderedQueryable<T> OrderBy<T>(IQueryable<T> source)
        {
            var querable = source as IQueryable<TSource>;
            if (querable == null)
                throw new ArgumentException($"Type mismatch between source and order expression. Source Generic Argument: {typeof(T).FullName}, Order Expression: {typeof(TSource).FullName}");

            if (!descending)
                return (IOrderedQueryable<T>)querable.OrderBy(expression);
            else
                return (IOrderedQueryable<T>)querable.OrderByDescending(expression);
        }
    }
}

