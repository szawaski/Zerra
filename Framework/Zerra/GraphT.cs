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
        public Graph(bool includeAllMembers) : base(includeAllMembers) { }

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

        private static bool TryReadMemberExpression(Expression expression, bool canCreateGraph, out string memberName, ref Graph graph)
        {
            //returning false means we did not have canCreateGraph OR the child was explicitly removed already.
            if (expression.NodeType != ExpressionType.Lambda || expression is not LambdaExpression lambda)
                throw new ArgumentException("Invalid member expression");
            var result = ReadMemberExpressionInnerLambda(lambda.Body, canCreateGraph, out var memberInfo, ref graph);
            memberName = memberInfo.Name;
            return result;
        }
        private static bool ReadMemberExpressionInnerLambda(Expression expression, bool canCreateGraph, out MemberInfo memberInfo, ref Graph graph)
        {
            if (expression.NodeType == ExpressionType.Call && expression is MethodCallExpression call)
            {
                if (call.Arguments.Count != 2 || call.Object is not null)
                    throw new ArgumentException("Invalid member expression");
                if (call.Arguments[0].NodeType != ExpressionType.MemberAccess || call.Arguments[0] is not MemberExpression member)
                    throw new ArgumentException("Invalid member expression");
                if (call.Arguments[1].NodeType != ExpressionType.Lambda || call.Arguments[1] is not LambdaExpression lambda)
                    throw new ArgumentException("Invalid member expression");
                if (member.Expression is null)
                    throw new ArgumentException("Invalid member expression");

                if (!ReadMemberExpressionInnerLambda(member, canCreateGraph, out memberInfo, ref graph))
                    return false;
                var childGraph = InternalGetChildGraph(graph, memberInfo, canCreateGraph);
                if (childGraph is null)
                    return false;
                graph = childGraph;

                if (!ReadMemberExpressionInnerLambda(lambda.Body, canCreateGraph, out memberInfo, ref graph))
                    return false;
                return true;
            }
            else
            {
                if (expression.NodeType == ExpressionType.Convert && expression is UnaryExpression convert)
                    expression = convert.Operand;
                if (expression.NodeType != ExpressionType.MemberAccess || expression is not MemberExpression member)
                    throw new ArgumentException("Invalid member expression");
                if (member.Expression is null)
                    throw new ArgumentException("Invalid member expression");

                if (member.Expression.NodeType != ExpressionType.Parameter)
                {
                    if (!ReadMemberExpressionInnerLambda(member.Expression, canCreateGraph, out memberInfo, ref graph))
                        return false;
                    var childGraph = InternalGetChildGraph(graph, memberInfo, canCreateGraph);
                    if (childGraph is null)
                        return false;
                    graph = childGraph;
                }

                memberInfo = member.Member;
                return true;
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

            foreach (var member in members)
            {
                Graph referenceGraph = this;
                if (!TryReadMemberExpression(member, true, out var memberName, ref referenceGraph))
                    continue;
                referenceGraph.AddMember(memberName);
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

            foreach (var member in members)
            {
                Graph referenceGraph = this;
                if (!TryReadMemberExpression(member, true, out var memberName, ref referenceGraph))
                    continue;
                referenceGraph.RemoveMember(memberName);
            }
            signature = null;
        }

        /// <summary>
        /// Adds a members to include in the graph.
        /// </summary>
        /// <param name="member">The member to include.</param>
        public void AddMember(Expression<Func<T, object?>> member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, true, out var memberName, ref referenceGraph))
                return;
            referenceGraph.AddMember(memberName);

            signature = null;
        }
        /// <summary>
        /// Removes a member from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="member">The member to remove.</param>
        public void RemoveMember(Expression<Func<T, object?>> member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, true, out var memberName, ref referenceGraph))
                return;
            referenceGraph.RemoveMember(memberName);

            signature = null;
        }
        /// <summary>
        /// Adds a child graph. If there is an existing child graph the members of the child will be merged.
        /// </summary>
        /// <param name="member">The member of the child graph.</param>
        /// <param name="graph">The child graph for the member.</param>
        public void AddChildGraph(Expression<Func<T, object?>> member, Graph graph)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, true, out var memberName, ref referenceGraph))
                return;
            referenceGraph.AddChildGraph(memberName, graph);

            signature = null;
        }
        /// <summary>
        /// Adds a child graph. If there is an existing child graph it will be replaced.
        /// </summary>
        /// <param name="member">The member of the child graph.</param>
        /// <param name="graph">The child graph for the member.</param>
        public void AddOrReplaceChildGraph(Expression<Func<T, object?>> member, Graph graph)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, true, out var memberName, ref referenceGraph))
                return;
            referenceGraph.AddOrReplaceChildGraph(memberName, graph);

            signature = null;
        }

        /// <summary>
        /// Indicates if the graph includes a member
        /// </summary>
        /// <param name="member">The member to see if it is included.</param>
        /// <returns>True if the graph has the member; otherwise, False.</returns>
        public bool HasMember(Expression<Func<T, object?>> member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));

            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, false, out var memberName, ref referenceGraph))
                return false;
            return referenceGraph.HasMember(memberName);
        }

        /// <summary>
        /// Returns the child graph of a member if the child graph exists.
        /// </summary>
        /// <param name="member">The member for the child graph.</param>
        /// <returns>The child graph of the member if it exists; otherwise, null.</returns>
        public Graph? GetChildGraph(Expression<Func<T, object?>> member)
        {
            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, false, out var memberName, ref referenceGraph))
                return null;
            return referenceGraph.GetChildGraph(memberName);
        }
        /// <summary>
        /// Returns the generic child graph of a member if the child graph exists.
        /// </summary>
        /// <param name="member">The member for the child graph.</param>
        /// <param name="type">The generic type of the child graph.</param>
        /// <returns>The child graph of the member if it exists; otherwise, null.</returns>
        public Graph? GetChildGraph(Expression<Func<T, object?>> member, Type type)
        {
            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, false, out var memberName, ref referenceGraph))
                return null;
            return referenceGraph.GetChildGraph(memberName, type);
        }
        /// <summary>
        /// Returns the generic child graph of a member if the child graph exists.
        /// </summary>
        /// <typeparam name="TGraph">The generic type of the child graph.</typeparam>
        /// <param name="member">The member for the child graph.</param>
        /// <returns>The child graph of the member if it exists; otherwise, null.</returns>
        public Graph<TGraph>? GetChildGraph<TGraph>(Expression<Func<T, object?>> member)
        {
            Graph referenceGraph = this;
            if (!TryReadMemberExpression(member, false, out var memberName, ref referenceGraph))
                return null;
            return referenceGraph.GetChildGraph<TGraph>(memberName);
        }

        /// <inheritdoc />
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