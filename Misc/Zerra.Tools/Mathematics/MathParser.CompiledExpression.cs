using System;
using System.Linq.Expressions;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        private sealed class CompiledExpression
        {
            public string ExpressionString { get; set; } = null!;
            public Delegate Expression { get; set; } = null!;
            public ParameterExpression[] Parameters { get; set; } = null!;
        }
    }
}
