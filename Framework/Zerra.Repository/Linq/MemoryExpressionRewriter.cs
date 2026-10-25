// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using System.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Helps with executing SQL like queries in LINQ handing case insensitivity and Span.Contains
    /// </summary>
    /// <remarks>
    /// C# 14 binds <c>array.Contains(value)</c> in an expression tree to <see cref="MemoryExtensions"/> over a <see cref="ReadOnlySpan{T}"/>.
    /// The expression interpreter, used when dynamic code isn't supported such as with AOT, can't call methods that take a span,
    /// so this rewrites those calls to search the array directly before a where or order expression is compiled to run in memory.
    /// The SQL converters don't need it, their evaluation unwraps the span conversion to the array.
    /// </remarks>
    internal sealed class MemoryExpressionRewriter : ExpressionVisitor
    {
        private static readonly MemoryExpressionRewriter instance = new(false);
        private static readonly MemoryExpressionRewriter instanceCaseSensitive = new(true);
        private static readonly MethodInfo arrayContainsMethod = typeof(MemoryExpressionRewriter).GetMethod(nameof(ArrayContains), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo stringEqualsMethod = typeof(MemoryExpressionRewriter).GetMethod(nameof(StringEquals), BindingFlags.NonPublic | BindingFlags.Static)!;

        private readonly bool caseSensitive;
        public MemoryExpressionRewriter(bool caseSensitive)
        {
            this.caseSensitive = caseSensitive;
        }

        /// <summary>
        /// Returns the expression with span based Contains calls replaced, or the same expression when there are none.
        /// </summary>
        public static TExpression Rewrite<TExpression>(TExpression expression, bool caseSensitive) where TExpression : Expression
            => caseSensitive ? (TExpression)instanceCaseSensitive.Visit(expression) : (TExpression)instance.Visit(expression);

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

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (!caseSensitive && node.NodeType == ExpressionType.Equal && node.Left.Type == typeof(string) && node.Right.Type == typeof(string))
            {
                var left = Visit(node.Left);
                var right = Visit(node.Right);
                return Expression.Call(stringEqualsMethod, left, right);
            }
            return base.VisitBinary(node);
        }

        //not generic so nothing needs to be generated for each element type, Array.IndexOf compares with Equals like Enumerable.Contains
        private static bool ArrayContains(Array array, object? value) => Array.IndexOf(array, value) >= 0;
        private static bool StringEquals(string s1, string s2) => String.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);
    }
}
