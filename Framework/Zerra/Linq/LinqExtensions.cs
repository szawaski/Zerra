// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Zerra.Linq;

namespace System.Linq
{
    public static class LinqExtensions
    {
        public static bool Contains<T>(this IEnumerable<T> it, IEnumerable<T> predicate)
        {
            foreach (var itemIt in it)
            {
                foreach (var itemPredicate in predicate)
                {
                    if (itemIt == null)
                    {
                        if (itemPredicate == null)
                            return true;
                    }
                    else if (itemPredicate != null && itemIt.Equals(itemPredicate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static void ForEach<T>(this IEnumerable<T> it, Action<T> predicate)
        {
            foreach (var item in it)
                predicate(item);
        }

        public static string ToLinqString(this Expression expression) { return LinqStringConverter.Convert(expression); }

        public static Expression<Func<T, bool>> AppendAnd<T>(this Expression<Func<T, bool>> it, params Expression<Func<T, bool>>[] expressions) { return LinqAppender.AppendAnd(it, expressions); }

        public static Expression<Func<T, bool>> AppendOr<T>(this Expression<Func<T, bool>> it, params Expression<Func<T, bool>>[] expressions) { return LinqAppender.AppendOr(it, expressions); }

        public static Expression<Func<T, bool>> AppendExpressionOnMember<T>(this Expression<Func<T, bool>> it, MemberInfo member, params Expression[] expressions) { return LinqAppender.AppendExpressionOnMember(it, member, expressions); }

        public static string ReadMemberName(this Expression it)
        {
            var exp = it is LambdaExpression lambda ? lambda.Body : it;

            if (exp.NodeType == ExpressionType.Convert)
            {
                var convert = (UnaryExpression)exp;
                exp = convert.Operand;
            }

            var stack = new Stack<string>();

            while (exp is MemberExpression member)
            {
                exp = member.Expression;
                stack.Push(member.Member.Name);
            }

            if (exp is not ParameterExpression)
                throw new Exception($"{nameof(ReadMemberName)} Invalid member expression");

            if (stack.Count == 1)
                return stack.Pop();

            var sb = new StringBuilder();
            while (stack.Count > 0)
            {
                if (sb.Length > 0)
                    _ = sb.Append('.');
                _ = sb.Append(stack.Pop());
            }

            return sb.ToString();
        }

        public static bool TryReadMemberName(this Expression it,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)] out string memberName
#else
            out string? memberName
#endif
        )
        {
            var exp = it is LambdaExpression lambda ? lambda.Body : it;

            if (exp.NodeType == ExpressionType.Convert)
            {
                var convert = (UnaryExpression)exp;
                exp = convert.Operand;
            }

            var stack = new Stack<string>();

            while (exp is MemberExpression member)
            {
                exp = member.Expression;
                stack.Push(member.Member.Name);
            }

            if (exp is not ParameterExpression)
            {
                memberName = null;
                return false;
            }

            if (stack.Count == 1)
            {
                memberName = stack.Pop();
                return true;
            }

            var sb = new StringBuilder();
            while (stack.Count > 0)
            {
                if (sb.Length > 0)
                    _ = sb.Append('.');
                _ = sb.Append(stack.Pop());
            }

            memberName = sb.ToString();
            return true;
        }
    }
}
