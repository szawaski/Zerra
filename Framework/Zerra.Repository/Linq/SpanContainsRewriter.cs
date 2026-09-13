// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// C# 14 binds <c>array.Contains(value)</c> in an expression tree to <see cref="MemoryExtensions"/> over a <see cref="ReadOnlySpan{T}"/>.
    /// The expression interpreter, used when dynamic code isn't supported such as with AOT, can't call methods that take a span,
    /// so this rewrites those calls to search the array directly before a where or order expression is compiled to run in memory.
    /// The SQL converters don't need it, their evaluation unwraps the span conversion to the array.
    /// </summary>
    internal sealed class SpanContainsRewriter : ExpressionVisitor
    {
        private static readonly SpanContainsRewriter instance = new();
        private static readonly MethodInfo arrayContainsMethod = typeof(SpanContainsRewriter).GetMethod(nameof(ArrayContains), BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <summary>
        /// Returns the expression with span based Contains calls replaced, or the same expression when there are none.
        /// </summary>
        public static TExpression Rewrite<TExpression>(TExpression expression) where TExpression : Expression
            => (TExpression)instance.Visit(expression);

        /// <inheritdoc />
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(MemoryExtensions)
                && node.Method.Name == nameof(MemoryExtensions.Contains)
                && node.Arguments.Count == 2
                && node.Arguments[0] is MethodCallExpression spanConversion
                && spanConversion.Method.Name == "op_Implicit"
                && spanConversion.Arguments.Count == 1
                && spanConversion.Arguments[0].Type.IsArray)
            {
                var array = Visit(spanConversion.Arguments[0]);
                var value = Visit(node.Arguments[1]);
                return Expression.Call(arrayContainsMethod, array, Expression.Convert(value, typeof(object)));
            }
            return base.VisitMethodCall(node);
        }

        //not generic so nothing needs to be generated for each element type, Array.IndexOf compares with Equals like Enumerable.Contains
        private static bool ArrayContains(Array array, object? value) => Array.IndexOf(array, value) >= 0;
    }
}
