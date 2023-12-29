// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public sealed class OrderExpression<TModel> : OrderExpression
        where TModel : class, new()
    {
        private OrderExpression(Expression expression, bool descending)
            : base(expression, descending) { }

        public static OrderExpression<TModel> Create<TKey>(Expression<Func<TModel, TKey>> expression)
        {
            return new OrderExpression<TModel>(expression, false);
        }
        public static OrderExpression<TModel> Create<TKey>(Expression<Func<TModel, TKey>> expression, bool descending)
        {
            return new OrderExpression<TModel>(expression, descending);
        }
    }
}

