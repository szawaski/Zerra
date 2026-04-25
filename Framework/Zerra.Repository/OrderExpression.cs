// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Represents an abstract ordering expression used to define sort criteria for queries.
    /// </summary>
    public abstract class OrderExpression
    {
        /// <summary>
        /// Gets the expression used to determine the ordering key.
        /// </summary>
        public abstract Expression Expression { get; }

        /// <summary>
        /// Gets a value indicating whether the ordering is descending.
        /// </summary>
        public abstract bool Descending { get; }

        /// <summary>
        /// Applies the ordering to an <see cref="IEnumerable{T}"/> source.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The source sequence to order.</param>
        /// <returns>An <see cref="IOrderedEnumerable{T}"/> whose elements are sorted according to this expression.</returns>
        public abstract IOrderedEnumerable<T> OrderBy<T>(IEnumerable<T> source) where T : class, new();

        /// <summary>
        /// Applies the ordering to an <see cref="IQueryable{T}"/> source.
        /// </summary>
        /// <typeparam name="T">The element type of the source sequence.</typeparam>
        /// <param name="source">The queryable source to order.</param>
        /// <returns>An <see cref="IOrderedQueryable{T}"/> whose elements are sorted according to this expression.</returns>
        public abstract IOrderedQueryable<T> OrderBy<T>(IQueryable<T> source) where T : class, new();
    }
}