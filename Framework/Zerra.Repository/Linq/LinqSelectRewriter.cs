// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// Writes an expression over the results of a Select in terms of the model, each member of a result being what the Select set it to.
    /// </summary>
    internal sealed class LinqSelectRewriter(ParameterExpression result, Expression value) : ExpressionVisitor
    {
        public static Expression Rewrite(LambdaExpression over, LambdaExpression select)
            => new LinqSelectRewriter(over.Parameters[0], select.Body).Visit(over.Body)!;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node != result)
                return node;
            if (IsObject(value))
                throw new NotSupportedException("A condition or order after a Select reads the members it set, not the whole result");
            return value;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var owner = Resolve(node.Expression);
            if (owner is null)
                return base.VisitMember(node);
            return IsObject(owner) ? Member(owner, node.Member.Name) : Expression.MakeMemberAccess(owner, node.Member);
        }

        //what an expression over the result is, while it may still be a whole object of the Select
        private Expression? Resolve(Expression? expression)
        {
            if (expression == result)
                return value;
            if (expression is MemberExpression member)
            {
                var owner = Resolve(member.Expression);
                if (owner is not null)
                    return IsObject(owner) ? Member(owner, member.Member.Name) : Expression.MakeMemberAccess(owner, member.Member);
            }
            return null;
        }

        private static bool IsObject(Expression expression) => expression is NewExpression or MemberInitExpression;

        private static Expression Member(Expression owner, string name)
        {
            var value = owner switch
            {
                NewExpression { Members: not null } created => FromConstructor(created, name),
                MemberInitExpression init => FromBindings(init, name),
                _ => null,
            } ?? throw new NotSupportedException($"{name} isn't set by the Select, so a condition or order after it can't read it");
            if (value.Type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(value.Type))
                throw new NotSupportedException($"{name} is rows read after the results, so filter or order by the relation before the Select");
            return value;
        }
        private static Expression? FromConstructor(NewExpression created, string name)
        {
            for (var i = 0; i < created.Members!.Count; i++)
            {
                if (created.Members[i].Name == name)
                    return created.Arguments[i];
            }
            return null;
        }
        private static Expression? FromBindings(MemberInitExpression init, string name)
        {
            foreach (var binding in init.Bindings)
            {
                if (binding.Member.Name == name && binding is MemberAssignment assignment)
                    return assignment.Expression;
            }
            return null;
        }
    }
}
