// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a strongly-typed ordering expression that sorts elements of <typeparamref name="TSource"/> by a key of type <typeparamref name="TKey"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements to be ordered.</typeparam>
    /// <typeparam name="TKey">The type of the key used for ordering.</typeparam>
    public sealed class OrderExpression<TSource, TKey> : OrderExpression
        where TSource : class, new()
    {
        private readonly Expression<Func<TSource, TKey>> expression;
        private readonly bool descending;

        /// <inheritdoc/>
        public override Expression Expression => expression;

        /// <inheritdoc/>
        public override bool Descending => descending;

        /// <summary>
        /// Initializes a new instance of <see cref="OrderExpression{TSource, TKey}"/>.
        /// </summary>
        /// <param name="expression">The key selector expression used to determine the ordering key.</param>
        /// <param name="descending">If <see langword="true"/>, the ordering is descending; otherwise ascending.</param>
        public OrderExpression(Expression<Func<TSource, TKey>> expression, bool descending)
        {
            this.expression = expression;
            this.descending = descending;
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is not compatible with <typeparamref name="TSource"/>.</exception>
        public override sealed IOrderedEnumerable<T> OrderBy<T>(IEnumerable<T> source)
        {
            var enumerable = source as IEnumerable<TSource>;
            if (enumerable == null)
                throw new ArgumentException($"Type mismatch between source and order expression. Source Generic Argument: {typeof(T).FullName}, Order Expression: {typeof(TSource).FullName}");

            if (!descending)
                return (IOrderedEnumerable<T>)enumerable.OrderBy(expression.Compile());
            else
                return (IOrderedEnumerable<T>)enumerable.OrderByDescending(expression.Compile());
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> is not compatible with <typeparamref name="TSource"/>.</exception>
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

