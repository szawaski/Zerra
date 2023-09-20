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

            public string Token { get; private set; }
            internal string TokenWithOpener { get; private set; }
            public LambdaExpression Operation { get; private set; }
            public MethodInfo InvokeMethod { get; private set; }
            public MethodOperator(string token, Expression<Func<double[], double>> operation)
            {
                this.Token = token;
                this.TokenWithOpener = token + ArgumentOpener;
                this.Operation = operation;
                this.InvokeMethod = operation.GetType().GetMethod("Invoke");
            }
            public MethodOperator(string token, LambdaExpression operation)
            {
                if (operation.ReturnType != typeof(double) || operation.Parameters.Count != 1 || operation.Parameters[0].Type != typeof(double[]))
                    throw new ArgumentException($"{nameof(LambdaExpression)} must be of Expression<Func<double[], double>>");
                this.Token = token;
                this.TokenWithOpener = token + ArgumentOpener;
                this.Operation = operation;
                this.InvokeMethod = operation.GetType().GetMethod("Invoke");
            }

            public override string ToString()
            {
                return Token;
            }
        }
    }
}
