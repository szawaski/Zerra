// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Zerra.Linq
{
    public sealed partial class WhereBuilder<TModel>
        where TModel : class, new()
    {
        private readonly List<ExpressionStackItem> expressionStack = new();

        public WhereBuilder(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, false, false, false, false));
        }

        public void And(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, true, false, false, false));
        }

        public void Or(Expression<Func<TModel, bool>> expression)
        {
            this.expressionStack.Add(new ExpressionStackItem(expression, false, true, false, false));
        }

        public void StartGroupAnd()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, true, false, true, false));
        }

        public void StartGroupOr()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, false, true, true, false));
        }

        public void EndGroup()
        {
            this.expressionStack.Add(new ExpressionStackItem(null, false, false, false, true));
        }

        public override string ToString()
        {
            var result = Build();
            return result.ToLinqString();
        }

        public Expression<Func<TModel, bool>> Build()
        {
            var parameter = Expression.Parameter(typeof(TModel), "$root");

            var i = 0;
            var exp = BuildGroup(ref i, parameter);
            exp ??= Expression.Constant(true, typeof(bool));

            var lambda = Expression.Lambda<Func<TModel, bool>>(exp, parameter);
            return lambda;
        }
        private Expression? BuildGroup(ref int i, ParameterExpression parameter)
        {
            Expression? exp = null;
            while (i < expressionStack.Count)
            {
                var item = expressionStack[i];
                if (item.EndGroup)
                {
                    break;
                }
                else if (exp is null)
                {
                    if (item.Expression is null)
                        throw new ArgumentException("Invalid Linq Where grouping structure");
                    exp = WhereBuilder<TModel>.Rebind(item.Expression, parameter);
                }
                else if (item.StartGroup)
                {
                    i++;
                    var group = BuildGroup(ref i, parameter);
                    if (group is not null)
                    {
                        if (item.And)
                        {
                            exp = Expression.AndAlso(exp, group);
                        }
                        else if (item.Or)
                        {
                            exp = Expression.OrElse(exp, group);
                        }
                    }
                }
                else if (item.And)
                {
                    exp = Expression.AndAlso(exp, WhereBuilder<TModel>.Rebind(item.Expression, parameter));
                }
                else if (item.Or)
                {
                    exp = Expression.OrElse(exp, WhereBuilder<TModel>.Rebind(item.Expression, parameter));
                }
                i++;
            }
            return exp;
        }
        private static Expression Rebind(Expression<Func<TModel, bool>> expression, ParameterExpression parameter)
        {
            return LinqRebinder.RebindExpression(expression.Body, expression.Parameters.First(), parameter);
        }
    }
}