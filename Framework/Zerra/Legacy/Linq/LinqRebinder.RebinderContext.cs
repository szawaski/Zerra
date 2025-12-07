// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Linq
{
    public sealed partial class LinqRebinder
    {
        private sealed class RebinderContext
        {
            //public IDictionary<Type, Type> TypeReplacements { get; }
            public IDictionary<Expression, Expression>? ExpressionReplacements { get; }
            public IDictionary<string, Expression>? ExpressionStringReplacements { get; }
            public IDictionary<Type, ParameterExpression> Parameters { get; }
            public RebinderContext(IDictionary<Expression, Expression>? expressionReplacements, IDictionary<string, Expression>? expressionStringReplacements)
            {
                //this.TypeReplacements = typeReplacements;
                this.ExpressionReplacements = expressionReplacements;
                this.ExpressionStringReplacements = expressionStringReplacements;
                this.Parameters = new Dictionary<Type, ParameterExpression>();
            }
        }
    }
}