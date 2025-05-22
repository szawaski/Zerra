using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        private sealed class MethodOperator
        {
            public static readonly char ArgumentOpener = '(';
            public static readonly char ArgumentCloser = ')';
            public static readonly char ArgumentSeperator = ',';

            public string Token { get; }
            public int MinArgumentCount { get; }
            public int MaxArgumentCount { get; }
            internal string TokenWithOpener { get; }
            public LambdaExpression Operation { get; }
            public MethodInfo InvokeMethod { get; }
            public MethodOperator(string token, int minArgumentCount, int maxArgumentCount, Expression<Func<double[], double>> operation)
            {
                this.Token = token;
                this.MinArgumentCount = minArgumentCount;
                this.MaxArgumentCount = maxArgumentCount;
                this.TokenWithOpener = token + ArgumentOpener;
                this.Operation = operation;
                this.InvokeMethod = operation.GetType().GetMethod("Invoke")!;
            }
            public MethodOperator(string token, int minArgumentCount, int maxArgumentCount, LambdaExpression operation)
            {
                if (operation.ReturnType != typeof(double) || operation.Parameters.Count != 1 || operation.Parameters[0].Type != typeof(double[]))
                    throw new ArgumentException($"{nameof(LambdaExpression)} must be of Expression<Func<double[], double>>");
                this.Token = token;
                this.MinArgumentCount = minArgumentCount;
                this.MaxArgumentCount = maxArgumentCount;
                this.TokenWithOpener = token + ArgumentOpener;
                this.Operation = operation;
                this.InvokeMethod = operation.GetType().GetMethod("Invoke")!;
            }

            public override string ToString()
            {
                return Token;
            }
        }
    }
}
