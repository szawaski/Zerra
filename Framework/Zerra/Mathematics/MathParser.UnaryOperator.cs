using System;
using System.Linq.Expressions;

namespace Zerra.Mathematics
{
    public partial class MathParser
    {
        private sealed class UnaryOperator
        {
            public string Token { get; private set; }
            public bool LeftSideOperand { get; private set; }
            public LambdaExpression Operation { get; private set; }
            public UnaryOperator(string tokens, bool leftSideOperand, Expression<Func<double, double>> operation)
            {
                this.Token = tokens;
                this.LeftSideOperand = leftSideOperand;
                this.Operation = operation;
            }
            public UnaryOperator(string tokens, bool leftSideOperand, LambdaExpression operation)
            {
                if (operation.ReturnType != typeof(double) || operation.Parameters.Count != 1 || operation.Parameters[0].Type != typeof(double))
                    throw new ArgumentException($"{nameof(LambdaExpression)} must be of Expression<Func<double, double>>");
                this.Token = tokens;
                this.LeftSideOperand = leftSideOperand;
                this.Operation = operation;
            }

            public override string ToString()
            {
                return Token;
            }
        }
    }
}
