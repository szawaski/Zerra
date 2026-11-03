// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.ComponentModel;
using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// One OrderBy, OrderByDescending, ThenBy, or ThenByDescending of a query.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqOrder
    {
        /// <summary>Gets the key to order by.</summary>
        public LambdaExpression Expression { get; }
        /// <summary>Gets whether the order is from the largest key down.</summary>
        public bool Descending { get; }

        /// <summary>Initializes an order.</summary>
        /// <param name="expression">The key to order by.</param>
        /// <param name="descending">Whether the order is from the largest key down.</param>
        public LinqOrder(LambdaExpression expression, bool descending)
        {
            this.Expression = expression;
            this.Descending = descending;
        }

        /// <inheritdoc/>
        public override string ToString() => Descending ? $"{Expression} desc" : Expression.ToString();
    }
}
