// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Linq
{
    public sealed partial class WhereBuilder<TModel>
        where TModel : class, new()
    {
        private readonly struct ExpressionStackItem
        {
            public readonly Expression<Func<TModel, bool>>? Expression;
            public readonly bool And;
            public readonly bool Or;
            public readonly bool StartGroup;
            public readonly bool EndGroup;

            public ExpressionStackItem(Expression<Func<TModel, bool>>? expression, bool and, bool or, bool startGroup, bool endGroup)
            {
                this.Expression = expression;
                this.And = and;
                this.Or = or;
                this.StartGroup = startGroup;
                this.EndGroup = endGroup;
            }
        }
    }
}