// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zerra.Linq
{
    public partial class LinqRebinder
    {
        private sealed class RebinderContext
        {
            //public IDictionary<Type, Type> TypeReplacements { get; private set; }
            public IDictionary<Expression, Expression> ExpressionReplacements { get; private set; }
            public IDictionary<string, Expression> ExpressionStringReplacements { get; private set; }
            public IDictionary<Type, ParameterExpression> Parameters { get; private set; }
            public RebinderContext(IDictionary<Expression, Expression> expressionReplacements, IDictionary<string, Expression> expressionStringReplacements)
            {
                //this.TypeReplacements = typeReplacements;
                this.ExpressionReplacements = expressionReplacements;
                this.ExpressionStringReplacements = expressionStringReplacements;
                this.Parameters = new Dictionary<Type, ParameterExpression>();
            }
        }
    }
}