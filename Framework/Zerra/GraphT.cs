// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Zerra
{
    /// <summary>
    /// A mapping of members and child members of an object to be used in a process.
    /// This generic version helps keeps the names of members consistent by using expressions.
    /// Specific graphs for different object instances can be also be mapped within a graph.
    /// </summary>
    /// <typeparam name="T">The object type for expressions to specify members.</typeparam>
    public sealed class Graph<T> : Graph
    {
        private Stack<MemberInfo>? memberInfoBuilder;
        private const int memberInfoBuilderDefaultCapacity = 4;

        /// <summary>
        /// Creates a Graph copy from another graph.
        /// </summary>
        /// <param name="graph">The graph to copy.</param>
        public Graph(Graph? graph)
            : base(graph)
        {
        }

        /// <summary>
        /// Creates an empty graph with no members included.
        /// </summary>
        public Graph() : this(false, (IEnumerable<Expression<Func<T, object?>>>?)null)
        {
            this.signature = "";
        }
        /// <summary>
        /// Creates an empty graph with the to option to include all memebers.
        /// </summary>
        /// <param name="includeAllMembers">Indiciates if all members should be included.</param>
        public Graph(bool includeAllMembers) : this(includeAllMembers, (IEnumerable<Expression<Func<T, object?>>>?)null)
        {
            this.signature = "A";
        }

        /// <summary>
        /// Creates a graph with the specified members included.
        /// </summary>
        /// <param name="members">The members to include</param>
        public Graph(params Expression<Func<T, object?>>[]? members) : this(false, (ICollection<Expression<Func<T, object?>>>?)members) { }
        /// <summary>
        /// Creates a graph with the specified members included.
        /// </summary>
        /// <param name="members">The members to include</param>
        public Graph(IEnumerable<Expression<Func<T, object?>>>? members) : this(false, members) { }
        /// <summary>
        /// Creates a graph with the to option to include all memebers and the specified members included.
        /// </summary>
        /// <param name="includeAllMembers">Indiciates if all members should be included.</param>
        /// <param name="members">The members to include</param>
        public Graph(bool includeAllMembers, params Expression<Func<T, object?>>[]? members) : this(includeAllMembers, (IEnumerable<Expression<Func<T, object?>>>?)members) { }
        /// <summary>
        /// Creates a graph with the to option to include all memebers and the specified members included.
        /// </summary>
        /// <param name="includeAllMembers">Indiciates if all members should be included.</param>
        /// <param name="members">The members to include</param>
        public Graph(bool includeAllMembers, IEnumerable<Expression<Func<T, object?>>>? members)
            : base(includeAllMembers, (IEnumerable<string>?)null)
        {
            if (members is not null)
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
                if (call.Arguments.Count != 2 || call.Object is not null)
                    throw new ArgumentException("Invalid member expression");
                if (call.Arguments[0].NodeType != ExpressionType.MemberAccess || call.Arguments[0] is not MemberExpression member)
                    throw new ArgumentException("Invalid member expression");
                if (call.Arguments[1].NodeType != ExpressionType.Lambda || call.Arguments[1] is not LambdaExpression lambda)
                    throw new ArgumentException("Invalid member expression");
                if (member.Expression is null)
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
                if (member.Expression is null)
                    throw new ArgumentException("Invalid member expression");

                memberInfos.Push(member.Member);
                ReadMemberExpressionMember(member.Expression, memberInfos);
            }
        }

        /// <summary>
        /// Adds members to include in the graph.
        /// </summary>
        /// <param name="members">The members to include.</param>
        public void AddMembers(params Expression<Func<T, object?>>[] members) => AddMembers((IEnumerable<Expression<Func<T, object?>>>)members);
        /// <summary>
        /// Adds members to include in the graph.
        /// </summary>
        /// <param name="members">The members to include.</param>
        public void AddMembers(IEnumerable<Expression<Func<T, object?>>> members)
        {
            if (members is null)
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

        /// <summary>
        /// Removes members from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="members">The members to remove.</param>
        public void RemoveMembers(params Expression<Func<T, object?>>[] members) => RemoveMembers((IEnumerable<Expression<Func<T, object?>>>)members);
        /// <summary>
        /// Removes members from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="members">The members to remove.</param>
        public void RemoveMembers(IEnumerable<Expression<Func<T, object?>>> members)
        {
            if (members is null)
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

        /// <summary>
        /// Adds a members to include in the graph.
        /// </summary>
        /// <param name="members">The member to include.</param>
        public void AddMember(Expression<Func<T, object?>> member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            memberInfoBuilder ??= new(memberInfoBuilderDefaultCapacity);

            ReadMemberExpression(member, memberInfoBuilder);
            AddMemberInfos(memberInfoBuilder);
            //stack will be empty at this point, no need to clear for reuse

            signature = null;
        }
        /// <summary>
        /// Removes a member from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="members">The member to remove.</param>
        public void RemoveMember(Expression<Func<T, object?>> member)
        {
            if (member is null)
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

        ///// <summary>
        ///// Generates a lambda expression that will create a new object of a different type based on the graph.
        ///// </summary>
        ///// <typeparam name="TSource">The new type of the object.</typeparam>
        ///// <returns>The lamba expression to create a new object.</returns>
        //public Expression<Func<TSource, T>> GenerateSelect<TSource>() { return GenerateSelect<TSource, T>(); }
    }
}