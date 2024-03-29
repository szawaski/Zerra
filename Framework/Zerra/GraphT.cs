﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Zerra
{
    public class Graph<T> : Graph
    {
        public Graph(Graph graph)
            : base(graph)
        {
        }

        public Graph() : this(null, false, (string[]?)null) { }
        public Graph(bool includeProperties) : this(null, includeProperties, (string[]?)null) { }
        public Graph(string? name, bool includeProperties) : this(name, includeProperties, (string[]?)null) { }
        public Graph(params string[] properties) : this(null, false, properties) { }
        public Graph(bool includeAllProperties, params string[]? properties) : this(null, includeAllProperties, properties) { }
        public Graph(string? name, bool includeAllProperties, params string[]? properties)
            : base(name, includeAllProperties, null)
        {
            this.type = GetModelType().FullName;

            if (properties != null)
                AddProperties(properties);
        }

        public Graph(params Expression<Func<T, object?>>[]? properties) : this(null, false, (ICollection<Expression<Func<T, object?>>>?)properties) { }
        public Graph(string? name, params Expression<Func<T, object?>>[]? properties) : this(name, false, (ICollection<Expression<Func<T, object?>>>?)properties) { }
        public Graph(bool includeAllProperties, params Expression<Func<T, object?>>[]? properties) : this(null, includeAllProperties, (ICollection<Expression<Func<T, object?>>>?)properties) { }
        public Graph(string? name, bool includeAllProperties, params Expression<Func<T, object?>>[]? properties) : this(name, includeAllProperties, (ICollection<Expression<Func<T, object?>>>?)properties) { }

        public Graph(IEnumerable<Expression<Func<T, object?>>>? properties) : this(null, false, properties) { }
        public Graph(string? name, IEnumerable<Expression<Func<T, object?>>>? properties) : this(name, false, properties) { }
        public Graph(bool includeAllProperties, IEnumerable<Expression<Func<T, object?>>>? properties) : this(null, includeAllProperties, properties) { }
        public Graph(string? name, bool includeAllProperties, IEnumerable<Expression<Func<T, object?>>>? properties)
            : base(name, includeAllProperties, (IReadOnlyCollection<string>?)null, null)
        {
            this.type = GetModelType().FullName;

            if (properties != null)
                AddProperties(properties);
        }

        private static void ReadPropertyExpression(Expression property, Stack<MemberInfo> members)
        {
            if (property.NodeType != ExpressionType.Lambda || property is not LambdaExpression lambda)
                throw new ArgumentException("Invalid property expression");
            ReadPropertyExpressionMember(lambda.Body, members);
        }
        private static void ReadPropertyExpressionMember(Expression property, Stack<MemberInfo> members)
        {
            if (property.NodeType == ExpressionType.Parameter)
            {
                return;
            }
            else if (property.NodeType == ExpressionType.Call && property is MethodCallExpression call)
            {
                if (call.Arguments.Count != 2 || call.Object != null)
                    throw new ArgumentException("Invalid property expression");
                if (call.Arguments[0].NodeType != ExpressionType.MemberAccess || call.Arguments[0] is not MemberExpression member)
                    throw new ArgumentException("Invalid property expression");
                if (call.Arguments[1].NodeType != ExpressionType.Lambda || call.Arguments[1] is not LambdaExpression lambda)
                    throw new ArgumentException("Invalid property expression");
                if (member.Expression == null)
                    throw new ArgumentException("Invalid property expression");

                ReadPropertyExpressionMember(lambda.Body, members);

                members.Push(member.Member);
                ReadPropertyExpressionMember(member.Expression, members);
            }
            else
            {
                if (property.NodeType == ExpressionType.Convert && property is UnaryExpression convert)
                {
                    property = convert.Operand;
                }
                if (property.NodeType != ExpressionType.MemberAccess || property is not MemberExpression member)
                    throw new ArgumentException("Invalid property expression");
                if (member.Expression == null)
                    throw new ArgumentException("Invalid property expression");

                members.Push(member.Member);
                ReadPropertyExpressionMember(member.Expression, members);
            }
        }

        public void AddProperties(params Expression<Func<T, object?>>[] properties) { AddProperties((IEnumerable<Expression<Func<T, object?>>>)properties); }
        public void AddProperties(IEnumerable<Expression<Func<T, object?>>> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var members = new Stack<MemberInfo>();
            foreach (var property in properties)
            {
                ReadPropertyExpression(property, members);
                AddMembers(members);
                members.Clear();
            }
            this.signature = null;
        }
        public void RemoveProperties(params Expression<Func<T, object?>>[] properties) { RemoveProperties((IEnumerable<Expression<Func<T, object?>>>)properties); }
        public void RemoveProperties(IEnumerable<Expression<Func<T, object?>>> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            var members = new Stack<MemberInfo>();
            foreach (var property in properties)
            {
                ReadPropertyExpression(property, members);
                RemoveMembers(members);
                members.Clear();
            }
            this.signature = null;
        }

        public new Graph<T> Copy()
        {
            return new Graph<T>(this);
        }

        public override Type GetModelType()
        {
            return typeof(T);
        }

        public Expression<Func<TSource, T>> GenerateSelect<TSource>() { return GenerateSelect<TSource, T>(); }
    }

    //public static class GraphExtensions
    //{
    //    public static T Graph<T>(this IEnumerable<T> it)
    //    {
    //        throw new InvalidOperationException($"{nameof(GraphExtensions)}.{nameof(Graph)} is an indicator and cannot be executed");
    //    }
    //}
}