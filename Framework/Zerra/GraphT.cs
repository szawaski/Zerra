// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Reflection;

namespace Zerra
{
    public sealed class Graph<T> : Graph
    {
        private Stack<MemberInfo>? memberInfoBuilder;
        private const int memberInfoBuilderDefaultCapacity = 4;

        public Graph(Graph? graph)
            : base(graph)
        {
        }

        public Graph() : this(false, (IEnumerable<Expression<Func<T, object?>>>?)null)
        {
            this.signature = "";
        }
        public Graph(bool includeAllMembers) : this(includeAllMembers, (IEnumerable<Expression<Func<T, object?>>>?)null)
        {
            this.signature = "A";
        }

        public Graph(params Expression<Func<T, object?>>[]? members) : this(false, (ICollection<Expression<Func<T, object?>>>?)members) { }
        public Graph(bool includeAllMembers, params Expression<Func<T, object?>>[]? members) : this(includeAllMembers, (IEnumerable<Expression<Func<T, object?>>>?)members) { }

        public Graph(IEnumerable<Expression<Func<T, object?>>>? members) : this(false, members) { }
        public Graph(bool includeAllMembers, IEnumerable<Expression<Func<T, object?>>>? members)
            : base(includeAllMembers, (IEnumerable<string>?)null)
        {
            if (members != null)
                AddMembers(members);
        }

        private static void ReadMemberExpression(Expression expression, Stack<MemberInfo> memberInfos)
        {
            if (expression.NodeType != ExpressionType.Lambda || expression is not LambdaExpression lambda)
                throw new ArgumentException("Invalid member expression");
            ReadMemberExpressionMember(lambda.Body, memberInfos);
        }
        private static void ReadMemberExpressionMember(Expression expression, Stack<MemberInfo> memberInfos)
        {
            if (expression.NodeType == ExpressionType.Parameter)
            {
                return;
            }
            else if (expression.NodeType == ExpressionType.Call && expression is MethodCallExpression call)
            {
                if (call.Arguments.Count != 2 || call.Object != null)
                    throw new ArgumentException("Invalid member expression");
                if (call.Arguments[0].NodeType != ExpressionType.MemberAccess || call.Arguments[0] is not MemberExpression member)
                    throw new ArgumentException("Invalid member expression");
                if (call.Arguments[1].NodeType != ExpressionType.Lambda || call.Arguments[1] is not LambdaExpression lambda)
                    throw new ArgumentException("Invalid member expression");
                if (member.Expression == null)
                    throw new ArgumentException("Invalid member expression");

                ReadMemberExpressionMember(lambda.Body, memberInfos);

                memberInfos.Push(member.Member);
                ReadMemberExpressionMember(member.Expression, memberInfos);
            }
            else
            {
                if (expression.NodeType == ExpressionType.Convert && expression is UnaryExpression convert)
                {
                    expression = convert.Operand;
                }
                if (expression.NodeType != ExpressionType.MemberAccess || expression is not MemberExpression member)
                    throw new ArgumentException("Invalid member expression");
                if (member.Expression == null)
                    throw new ArgumentException("Invalid member expression");

                memberInfos.Push(member.Member);
                ReadMemberExpressionMember(member.Expression, memberInfos);
            }
        }

        public void AddMembers(params Expression<Func<T, object?>>[] members) => AddMembers((IEnumerable<Expression<Func<T, object?>>>)members);
        public void AddMembers(IEnumerable<Expression<Func<T, object?>>> members)
        {
            if (members == null)
                throw new ArgumentNullException(nameof(members));

            memberInfoBuilder ??= new(memberInfoBuilderDefaultCapacity);

            foreach (var member in members)
            {
                ReadMemberExpression(member, memberInfoBuilder);
                AddMemberInfos(memberInfoBuilder);
                //stack will be empty at this point, no need to clear for reuse
            }
            signature = null;
        }
        public void RemoveMembers(params Expression<Func<T, object?>>[] members) => RemoveMembers((IEnumerable<Expression<Func<T, object?>>>)members);
        public void RemoveMembers(IEnumerable<Expression<Func<T, object?>>> members)
        {
            if (members == null)
                throw new ArgumentNullException(nameof(members));

            memberInfoBuilder ??= new(memberInfoBuilderDefaultCapacity);

            foreach (var member in members)
            {
                ReadMemberExpression(member, memberInfoBuilder);
                RemoveMemberInfos(memberInfoBuilder);
                //stack will be empty at this point, no need to clear for reuse
            }
            signature = null;
        }

        public void AddMember(Expression<Func<T, object?>> member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            memberInfoBuilder ??= new(memberInfoBuilderDefaultCapacity);

            ReadMemberExpression(member, memberInfoBuilder);
            AddMemberInfos(memberInfoBuilder);
            //stack will be empty at this point, no need to clear for reuse

            signature = null;
        }
        public void RemoveMember(Expression<Func<T, object?>> member)
        {
            if (member == null)
                throw new ArgumentNullException(nameof(member));

            memberInfoBuilder ??= new(memberInfoBuilderDefaultCapacity);

            ReadMemberExpression(member, memberInfoBuilder);
            RemoveMemberInfos(memberInfoBuilder);
            //stack will be empty at this point, no need to clear for reuse

            signature = null;
        }

        protected override Type GetModelType()
        {
            return typeof(T);
        }

        public Expression<Func<TSource, T>> GenerateSelect<TSource>() { return GenerateSelect<TSource, T>(); }
    }
}