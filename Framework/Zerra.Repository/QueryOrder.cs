// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;

namespace Zerra.Repository
{
    public abstract class QueryOrder
    {
        public OrderExpression[] OrderExpressions { get; protected set; }

        public QueryOrder(OrderExpression[] expressions)
        {
            this.OrderExpressions = expressions;
        }

        public override string ToString()
        {
            return String.Join(",", this.OrderExpressions.Select(x => x.ToString()));
        }
    }
}

