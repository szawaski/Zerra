using System;
using System.Linq.Expressions;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        private sealed class TertiaryOperator
        {
            public string Token1 { get; }
            public string Token2 { get; }
            public LambdaExpression Operation { get; }
            public TertiaryOperator(string token1, string token2, Expression<Func<double, double, double, double>> operation)
            {
                this.Token1 = token1;
                this.Token2 = token2;
                this.Operation = operation;
            }
            public TertiaryOperator(string token1, string token2, LambdaExpression operation)
            {
                if (operation.ReturnType != typeof(double) || operation.Parameters.Count != 3 || operation.Parameters[0].Type != typeof(double) || operation.Parameters[1].Type != typeof(double) || operation.Parameters[2].Type != typeof(double))
                    throw new ArgumentException($"{nameof(LambdaExpression)} must be of Expression<Func<double, double, double, double>>");
                this.Token1 = token1;
                this.Token2 = token2;
                this.Operation = operation;
            }

            public override string ToString()
            {
                return Token1 + Token2;
            }
        }
    }
}
