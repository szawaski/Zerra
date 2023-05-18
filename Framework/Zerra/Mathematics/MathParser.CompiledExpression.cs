using System;
using System.Linq.Expressions;

namespace Zerra.Mathematics
{
    public partial class MathParser
    {
        private sealed class CompiledExpression
        {
            public string ExpressionString { get; set; }
            public Delegate Expression { get; set; }
            public ParameterExpression[] Parameters { get; set; }
        }
    }
}
