﻿using System;
using System.Linq.Expressions;

namespace Zerra.Mathematics
{
    public sealed partial class MathParser
    {
        private sealed class BinaryOperator
        {
            public string Token { get; }
            public LambdaExpression Operation { get; }
            public BinaryOperator(string token, Expression<Func<double, double, double>> operation)
            {
                this.Token = token;
                this.Operation = operation;
            }
            public BinaryOperator(string token, LambdaExpression operation)
            {
                if (operation.ReturnType != typeof(double) || operation.Parameters.Count != 2 || operation.Parameters[0].Type != typeof(double) || operation.Parameters[1].Type != typeof(double))
                    throw new ArgumentException($"{nameof(LambdaExpression)} must be of Expression<Func<double, double, double>>");
                this.Token = token;
                this.Operation = operation;
            }

            public override string ToString()
            {
                return Token;
            }
        }
    }
}
