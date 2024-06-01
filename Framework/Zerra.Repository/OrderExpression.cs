// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Linq;

namespace Zerra.Repository
{
    public abstract class OrderExpression
    {
        public Expression Expression { get; protected set; }
        public bool Descending { get; protected set; }

        public OrderExpression(Expression expression, bool descending)
        {
            this.Expression = expression;
            this.Descending = descending;
        }

        public override string? ToString()
        {
            return this.Expression.ToLinqString() + (this.Descending ? " DESC" : null);
        }
    }
}

