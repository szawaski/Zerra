// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryOrder
    {
        public OrderExpression[] OrderExpressions { get; protected set; }

        public override string ToString()
        {
            return String.Join(",", this.OrderExpressions.Select(x => x.ToString()));
        }
    }

    public class QueryOrder<TModel> : QueryOrder
        where TModel : class, new()
    {
        public QueryOrder(params OrderExpression<TModel>[] expressions)
        {
            this.OrderExpressions = expressions;
        }

        public static QueryOrder<TModel> Create<T1>(Expression<Func<TModel, T1>> expression1)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false)
            });
        }
        public static QueryOrder<TModel> Create<T1>(Expression<Func<TModel, T1>> expression1, bool descending1)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1)
            });
        }

        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false),
                OrderExpression<TModel>.Create(expression2, false)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, bool descending2)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false),
                OrderExpression<TModel>.Create(expression2, descending2)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1),
                OrderExpression<TModel>.Create(expression2, false)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, bool descending2)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1),
                OrderExpression<TModel>.Create(expression2, descending2)
            });
        }

        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false),
                OrderExpression<TModel>.Create(expression2, false),
                OrderExpression<TModel>.Create(expression3, false)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1),
                OrderExpression<TModel>.Create(expression2, false),
                OrderExpression<TModel>.Create(expression3, false)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1),
                OrderExpression<TModel>.Create(expression2, descending2),
                OrderExpression<TModel>.Create(expression3, false)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1),
                OrderExpression<TModel>.Create(expression2, false),
                OrderExpression<TModel>.Create(expression3, descending3)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false),
                OrderExpression<TModel>.Create(expression2, descending2),
                OrderExpression<TModel>.Create(expression3, descending3)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false),
                OrderExpression<TModel>.Create(expression2, descending2),
                OrderExpression<TModel>.Create(expression3, false)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, Expression<Func<TModel, T2>> expression2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, false),
                OrderExpression<TModel>.Create(expression2, false),
                OrderExpression<TModel>.Create(expression3, descending3)
            });
        }
        public static QueryOrder<TModel> Create<T1, T2, T3>(Expression<Func<TModel, T1>> expression1, bool descending1, Expression<Func<TModel, T2>> expression2, bool descending2, Expression<Func<TModel, T3>> expression3, bool descending3)
        {
            return new QueryOrder<TModel>(new OrderExpression<TModel>[]
            {
                OrderExpression<TModel>.Create(expression1, descending1),
                OrderExpression<TModel>.Create(expression2, descending2),
                OrderExpression<TModel>.Create(expression3, descending3)
            });
        }
    }

    public abstract class OrderExpression
    {
        public Expression Expression { get; protected set; }
        public bool Descending { get; protected set; }

        public override string ToString()
        {
            if (this.Expression == null)
                return null;

            return this.Expression.ToLinqString() + (this.Descending ? " DESC" : null);
        }
    }

    public class OrderExpression<TModel> : OrderExpression
        where TModel : class, new()
    {
        private OrderExpression(Expression expression, bool descending)
        {
            this.Expression = expression;
            this.Descending = descending;
        }

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

