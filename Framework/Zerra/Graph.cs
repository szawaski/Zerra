﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Zerra.Reflection;

namespace Zerra
{
    /// <summary>
    /// A mapping of members and child members of an object to be used in a process.
    /// Members indiciated are strings and not enforced to match the object.  Use the generic <see cref="Graph{T}" /> to help enforce correct naming.
    /// Specific graphs for different object instances can be also be mapped within a graph.
    /// </summary>
    public class Graph
    {
        protected bool includeAllMembers;
        protected HashSet<string>? addedMembers;
        protected HashSet<string>? removedMembers;
        protected Dictionary<string, Graph>? childGraphs;
        protected Dictionary<object, Graph>? instanceGraphs;

        /// <summary>
        /// All members are included unless explicity removed.
        /// </summary>
        public bool IncludeAllMembers
        {
            get => includeAllMembers;
            set
            {
                includeAllMembers = value;
                signature = null;
            }
        }

        public IReadOnlyCollection<Graph> ChildGraphs => childGraphs is null ? Array.Empty<Graph>() : childGraphs.Values;

        private static readonly TypeDetail graphTType = typeof(Graph<>).GetTypeDetail();

        [NonSerialized]
        protected string? signature = null;
        /// <summary>
        /// The unique signature of the graph used for comparing graphs.
        /// </summary>
        public string Signature
        {
            get
            {
                if (this.signature is null)
                {
                    GenerateSignature();
                }
                return this.signature!;
            }
        }

        /// <summary>
        /// Creates a Graph copy from another graph.
        /// </summary>
        /// <param name="graph">The graph to copy.</param>
        public Graph(Graph? graph)
        {
            if (graph is not null)
            {
                this.includeAllMembers = graph.includeAllMembers;
                if (graph.addedMembers is not null)
                    this.addedMembers = new(graph.addedMembers);
                if (graph.removedMembers is not null)
                    this.removedMembers = new(graph.removedMembers);
                if (graph.childGraphs is not null)
                    this.childGraphs = new(graph.childGraphs);
                this.signature = graph.signature;
            }
            else
            {
                this.signature = "";
            }
        }

        /// <summary>
        /// Creates an empty graph with no members included.
        /// </summary>
        public Graph()
        {
            this.signature = "";
        }
        /// <summary>
        /// Creates an empty graph with the to option to include all memebers.
        /// </summary>
        /// <param name="includeAllMembers">Indiciates if all members should be included.</param>
        public Graph(bool includeAllMembers)
        {
            this.includeAllMembers = true;
            this.signature = "A";
        }
        /// <summary>
        /// Creates a graph with the specified members included.
        /// </summary>
        /// <param name="members">The members to include</param>
        public Graph(params string[] members) : this(false, members) { }
        /// <summary>
        /// Creates a graph with the specified members included.
        /// </summary>
        /// <param name="members">The members to include</param>
        public Graph(IEnumerable<string>? members) : this(false, members) { }
        protected Graph(bool includeAllMembers, IEnumerable<string>? members)
        {
            this.includeAllMembers = includeAllMembers;

            if (members is not null)
                AddMembers(members);
        }

        /// <summary>
        /// Indicates that the graph has no members included.
        /// </summary>
        public bool IsEmpty => !includeAllMembers && (addedMembers?.Count ?? 0) == 0 && (childGraphs?.Count ?? 0) == 0;

        /// <summary>
        /// Determines if two graphs are equal. Graphs with instances cannot be compared.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if the graphs are equal; otherwise false.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is not Graph objCasted)
                return false;
            return this.Signature == objCasted.Signature;
        }
        /// <summary>
        /// Gets the Hash Code for a graph.  Graphs with instances cannot use Hash Codes.
        /// </summary>
        /// <returns>The Hash Code of the graph.</returns>
        public override int GetHashCode()
        {
            return this.Signature.GetHashCode();
        }
        /// <summary>
        /// Determines if two graphs are equal. Graphs with instances cannot be compared.
        /// </summary>
        /// <param name="graph1">The first graph to compare.</param>
        /// <param name="graph2">The second graph to compare.</param>
        /// <returns>True if the graphs are equal; otherwise false.</returns>
        public static bool operator ==(Graph? graph1, Graph? graph2)
        {
            if (graph1 is null)
                return graph2 is null;
            if (graph2 is null)
                return false;
            return graph1.Signature == graph2.Signature;
        }
        /// <summary>
        /// Determines if two graphs are not equal. Graphs with instances cannot be compared.
        /// </summary>
        /// <param name="graph1">The first graph to compare.</param>
        /// <param name="graph2">The second graph to compare.</param>
        /// <returns>True if the graphs are not equal; otherwise false.</returns>
        public static bool operator !=(Graph? graph1, Graph? graph2)
        {
            if (graph1 is null)
                return graph2 is not null;
            if (graph2 is null)
                return true;
            return graph1.Signature != graph2.Signature;
        }

        private void GenerateSignature()
        {
            var sb = new StringBuilder();
            GenerateSignatureBuilder(sb);
            signature = sb.ToString();
        }
        private void GenerateSignatureBuilder(StringBuilder sb)
        {
            if (instanceGraphs is not null)
                throw new InvalidOperationException("Graphs with instances cannot be compared");

            if (includeAllMembers)
                _ = sb.Append("A:");

            if (addedMembers is not null)
            {
                foreach (var member in addedMembers.OrderBy(x => x))
                {
                    _ = sb.Append("P:");
                    _ = sb.Append(member);
                }
            }

            if (includeAllMembers && removedMembers is not null)
            {
                foreach (var member in removedMembers.OrderBy(x => x))
                {
                    _ = sb.Append("R:");
                    _ = sb.Append(member);
                }
            }

            if (childGraphs is not null)
            {
                foreach (var graph in childGraphs.OrderBy(x => x.Key))
                {
                    _ = sb.Append("G:(");
                    graph.Value.GenerateSignatureBuilder(sb);
                    _ = sb.Append(")");
                }
            }
        }

        /// <summary>
        /// Adds members to include in the graph.
        /// </summary>
        /// <param name="members">The members to include.</param>
        public void AddMembers(params string[] members) => AddMembers((IEnumerable<string>)members);
        /// <summary>
        /// Adds members to include in the graph.
        /// </summary>
        /// <param name="members">The members to include.</param>
        public void AddMembers(IEnumerable<string> members)
        {
            if (members is null)
                throw new ArgumentNullException(nameof(members));

            foreach (var member in members)
                AddMember(member);
        }

        /// <summary>
        /// Removes members from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="members">The members to remove.</param>
        public void RemoveMembers(params string[] members) => RemoveMembers((IEnumerable<string>)members);
        /// <summary>
        /// Removes members from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="members">The members to remove.</param>
        public void RemoveMembers(IEnumerable<string> members)
        {
            if (members is null)
                throw new ArgumentNullException(nameof(members));

            foreach (var member in members)
                RemoveMember(member);
        }

        /// <summary>
        /// Adds a members to include in the graph.
        /// </summary>
        /// <param name="members">The member to include.</param>
        public void AddMember(string member)
        {
            if (String.IsNullOrWhiteSpace(member))
                throw new ArgumentNullException(nameof(member));

            if (childGraphs is not null && childGraphs.TryGetValue(member, out var childGraph))
            {
                childGraph.includeAllMembers = true;
            }
            else
            {
                addedMembers ??= new();
                _ = addedMembers.Add(member);
            }

            signature = null;

        }
        /// <summary>
        /// Removes a member from the graph. This overrides IncludeAllMembers.
        /// </summary>
        /// <param name="members">The member to remove.</param>
        public void RemoveMember(string member)
        {
            if (String.IsNullOrWhiteSpace(member))
                throw new ArgumentNullException(nameof(member));

            _ = addedMembers?.Remove(member);
            removedMembers ??= new HashSet<string>();
            _ = removedMembers?.Add(member);
            _ = childGraphs?.Remove(member);

            signature = null;
        }
        /// <summary>
        /// Adds a child graph. If there is an existing child graph the members of the child will be merged.
        /// </summary>
        /// <param name="members">The member of the child graph.</param>
        /// <param name="graph">The child graph for the member.</param>
        public void AddChildGraph(string member, Graph graph)
        {
            if (String.IsNullOrWhiteSpace(member))
                throw new InvalidOperationException("Cannot add a child graph without a name.");

            if (addedMembers?.Remove(member) == true)
                graph.IncludeAllMembers = true;

            childGraphs ??= new();
            if (childGraphs.TryGetValue(member, out var existingGraph))
            {
                existingGraph.includeAllMembers |= graph.includeAllMembers;
                if (graph.addedMembers is not null)
                    existingGraph.AddMembers(graph.addedMembers);
                if (graph.removedMembers is not null)
                    existingGraph.RemoveMembers(graph.removedMembers);
                if (graph.childGraphs is not null)
                {
                    foreach (var childGraphs in graph.childGraphs)
                        existingGraph.AddChildGraph(childGraphs.Key, childGraphs.Value);
                }
                if (graph.instanceGraphs is not null)
                {
                    foreach (var instanceGraph in graph.instanceGraphs)
                        existingGraph.AddInstanceGraph(instanceGraph.Key, instanceGraph.Value);
                }
            }
            else
            {
                childGraphs.Add(member, graph);
            }

            _ = removedMembers?.Remove(member);

            signature = null;
        }
        /// <summary>
        /// Adds a child graph. If there is an existing child graph it will be replaced.
        /// </summary>
        /// <param name="members">The member of the child graph.</param>
        /// <param name="graph">The child graph for the member.</param>
        public void AddOrReplaceChildGraph(string member, Graph graph)
        {
            if (String.IsNullOrWhiteSpace(member))
                throw new InvalidOperationException("Cannot add a graph without a member name.");

            if (addedMembers?.Remove(member) == true)
                graph.IncludeAllMembers = true;

            childGraphs ??= new();
            childGraphs[member] = graph;

            _ = removedMembers?.Remove(member);

            signature = null;
        }

        /// <summary>
        /// Adds a child graph that is specific for an instance. This graph no longer able to be compared with other graphs.
        /// </summary>
        /// <param name="instance">The instance for the graph.</param>
        /// <param name="graph">The graph for the specific instance.</param>
        public void AddInstanceGraph(object instance, Graph graph)
        {
            if (graph.instanceGraphs is not null)
                throw new InvalidOperationException("Graph being added already has instances");

            instanceGraphs ??= new();
            instanceGraphs[instance] = graph;
            graph.instanceGraphs = this.instanceGraphs;
            graph.signature = null;

            signature = null;
        }
        /// <summary>
        /// Removes a child graph that is specific for an instance.
        /// </summary>
        /// <param name="instance">The instance for whoms graph should be removed.</param>
        public void RemoveInstanceGraph(object instance)
        {
            if (instanceGraphs is null)
                return;
            _ = instanceGraphs.Remove(instance);
            signature = null;
        }

        protected void AddMemberInfos(Stack<MemberInfo> members)
        {
            if (members.Count == 0)
                return;

            var member = members.Pop();
            if (members.Count == 0)
            {
                AddMember(member.Name);
            }
            else
            {
                childGraphs ??= new();
                if (!childGraphs.TryGetValue(member.Name, out var childGraph))
                {
                    if (member.MemberType == MemberTypes.Property)
                    {
                        var graphTTypeGeneric = graphTType.GetGenericTypeDetail(((PropertyInfo)member).PropertyType);
                        childGraph = (Graph)graphTTypeGeneric.CreatorBoxed();
                    }
                    else
                    {
                        var graphTTypeGeneric = graphTType.GetGenericTypeDetail(((FieldInfo)member).FieldType);
                        childGraph = (Graph)graphTTypeGeneric.CreatorBoxed();
                    }

                    childGraphs.Add(member.Name, childGraph);
                }
                if (this.addedMembers is not null && this.addedMembers.Contains(member.Name))
                {
                    childGraph.includeAllMembers = true;
                    _ = this.addedMembers.Remove(member.Name);
                    childGraph.signature = null;
                }
                childGraph.AddMemberInfos(members);
            }
        }
        protected void RemoveMemberInfos(Stack<MemberInfo> members)
        {
            if (members.Count == 0)
                return;

            var member = members.Pop();
            if (members.Count == 0)
            {
                RemoveMember(member.Name);
            }
            else
            {
                if (childGraphs is not null && childGraphs.TryGetValue(member.Name, out var childGraph))
                    childGraph.RemoveMemberInfos(members);
            }
        }

        /// <summary>
        /// Indicates if the graph includes a member
        /// </summary>
        /// <param name="member">The member to see if it is included.</param>
        /// <returns>True if the graph has the member; otherwise, False.</returns>
        public bool HasMember(string member)
        {
            return (includeAllMembers && (removedMembers is null || !removedMembers.Contains(member))) || (addedMembers is not null && addedMembers.Contains(member)) || (childGraphs is not null && childGraphs.ContainsKey(member));
        }

        /// <summary>
        /// Returns the child graph of a member if the child graph exists.
        /// </summary>
        /// <param name="member">The member for the child graph.</param>
        /// <returns>The child graph of the member if it exists; otherwise, null.</returns>
        public Graph? GetChildGraph(string member)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(member, out var childGraph))
                return null;
            return childGraph;
        }
        /// <summary>
        /// Returns the generic child graph of a member if the child graph exists.
        /// </summary>
        /// <param name="member">The member for the child graph.</param>
        /// <param name="type">The generic type of the child graph.</param>
        /// <returns>The child graph of the member if it exists; otherwise, null.</returns>
        public Graph? GetChildGraph(string member, Type type)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(member, out var nonGenericGraph))
                return null;
            if (nonGenericGraph.GetModelType() == type)
                return (Graph)System.Convert.ChangeType(nonGenericGraph, graphTType.GetGenericTypeDetail(type).Type);
            return Convert(nonGenericGraph, type);
        }
        /// <summary>
        /// Returns the generic child graph of a member if the child graph exists.
        /// </summary>
        /// <typeparam name="T">The generic type of the child graph.</typeparam>
        /// <param name="member">The member for the child graph.</param>
        /// <returns>The child graph of the member if it exists; otherwise, null.</returns>
        public Graph<T>? GetChildGraph<T>(string member)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(member, out var nonGenericGraph))
                return null;
            if (nonGenericGraph.GetModelType() == typeof(T))
                return (Graph<T>)nonGenericGraph;
            return new Graph<T>(nonGenericGraph);
        }

        /// <summary>
        /// Returns the graph specific for an instance. If there is none, this graph itself will be returned.
        /// </summary>
        /// <param name="instance">The instance for whoms graph should be returned.</param>
        /// <returns>The graph for the instance.</returns>
        public Graph GetInstanceGraph(object instance)
        {
            if (instanceGraphs is not null && instanceGraphs.TryGetValue(instance, out var instanceGraph))
                return instanceGraph;
            return this;
        }
        /// <summary>
        /// Returns the child graph specific for an instance. If there is none, the containing child graph will be returned.
        /// </summary>
        /// <param name="member">The member for the child graph.</param>
        /// <param name="instance">The instance for whoms graph should be returned.</param>
        /// <returns>The child graph for the instance.</returns>
        public Graph? GetChildInstanceGraph(string member, object instance)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(member, out var childGraph))
                return null;

            if (childGraph.instanceGraphs is not null && childGraph.instanceGraphs.TryGetValue(instance, out var instanceGraph))
                return instanceGraph;
            return childGraph;
        }
        /// <summary>
        /// Returns the generic child graph specific for an instance. If there is none, the containing child graph will be returned.
        /// </summary>
        /// <param name="member">The member for the child graph.</param>
        /// <param name="type">The generic type of the child graph.</param>
        /// <param name="instance">The instance for whoms graph should be returned.</param>
        /// <returns>The generic child graph for the instance.</returns>
        public Graph? GetChildInstanceGraph(string member, Type type, object instance)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(member, out var nonGenericGraph))
                return null;
            if (nonGenericGraph.GetModelType() == type)
                return nonGenericGraph;

            if (nonGenericGraph.instanceGraphs is not null && nonGenericGraph.instanceGraphs.TryGetValue(instance, out var instanceGraph))
                return Convert(instanceGraph, type);
            return Convert(nonGenericGraph, type);
        }
        /// <summary>
        /// Returns the generic child graph specific for an instance. If there is none, the containing child graph will be returned.
        /// </summary>
        /// <typeparam name="T">The generic type of the child graph.</typeparam>
        /// <param name="member">The member for the child graph.</param>
        /// <param name="instance">The instance for whoms graph should be returned.</param>
        /// <returns>The generic child graph for the instance.</returns>
        public Graph<T>? GetChildInstanceGraph<T>(string member, object instance)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(member, out var nonGenericGraph))
                return null;

            if (nonGenericGraph.instanceGraphs is not null && nonGenericGraph.instanceGraphs.TryGetValue(instance, out var instanceGraph))
            {
                if (instanceGraph.GetModelType() == typeof(T))
                    return (Graph<T>)instanceGraph;
                return new Graph<T>(instanceGraph);
            }
            if (nonGenericGraph.GetModelType() == typeof(T))
                return (Graph<T>)nonGenericGraph;
            return new Graph<T>(nonGenericGraph);
        }

        private static Graph Convert(Graph graph, Type type)
        {
            var constructor = TypeAnalyzer.GetGenericTypeDetail(typeof(Graph<>), type).GetConstructorBoxed([typeof(Graph)]);
            var genericGraph = (Graph)constructor.CreatorWithArgsBoxed([graph]);
            return genericGraph;
        }

        /// <summary>
        /// Generates a string representation of the members of this graph.
        /// </summary>
        /// <returns>A string representation of the graph.</returns>
        public override string ToString()
        {
            if (instanceGraphs is not null)
                return "Graph has instances";

            var sb = new StringBuilder();
            ToString(sb, 0);
            return sb.ToString();
        }
        private void ToString(StringBuilder sb, int depth)
        {
            if (includeAllMembers)
            {
                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine);
                for (var i = 0; i < depth; i++)
                    _ = sb.Append("  ");

                _ = sb.Append("[ALL]");
            }
            if (addedMembers is not null)
            {
                foreach (var member in addedMembers)
                {
                    if (removedMembers is not null && removedMembers.Contains(member))
                        continue;

                    if (sb.Length > 0)
                        _ = sb.Append(Environment.NewLine);
                    for (var i = 0; i < depth; i++)
                        _ = sb.Append("  ");

                    _ = sb.Append(member);
                }
            }
            if (childGraphs is not null)
            {
                foreach (var childGraph in childGraphs)
                {
                    if (sb.Length > 0)
                        _ = sb.Append(Environment.NewLine);
                    if (String.IsNullOrEmpty(childGraph.Key))
                        continue;

                    for (var i = 0; i < depth; i++)
                        _ = sb.Append("  ");

                    _ = sb.Append(childGraph.Key);
                    childGraph.Value.ToString(sb, depth + 1);
                }
            }
        }

        protected virtual Type? GetModelType() => null;

        //private static readonly byte maxRecursiveDepth = 2;
        //private static readonly MethodInfo selectMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "Select" && m.GetParameters().Length == 2).First();
        //private static readonly MethodInfo listMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToList" && m.GetParameters().Length == 1).First();
        //private static readonly MethodInfo arrayMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToArray" && m.GetParameters().Length == 1).First();
        //private static readonly MethodInfo hashSetMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToHashSet" && m.GetParameters().Length == 1).First();
        //private static readonly ConcurrentFactoryDictionary<TypeKey, Expression> selectExpressions = new();
        ///// <summary>
        ///// Generates a lambda expression that will create a new object of a different type based on the graph.
        ///// </summary>
        ///// <typeparam name="TSource">The original type of the object.</typeparam>
        ///// <typeparam name="TTarget">The new type of the object.</typeparam>
        ///// <returns>The lamba expression to create a new object.</returns>
        //public Expression<Func<TSource, TTarget>> GenerateSelect<TSource, TTarget>()
        //{
        //    var key = new TypeKey(Signature, typeof(TSource), typeof(TTarget));

        //    var expression = (Expression<Func<TSource, TTarget>>)selectExpressions.GetOrAdd(key, (_) =>
        //    {
        //        return GenerateSelectorExpression<TSource, TTarget>(this);
        //    });

        //    return expression;
        //}
        //private static Expression<Func<TSource, TTarget>> GenerateSelectorExpression<TSource, TTarget>(Graph? graph)
        //{
        //    var typeSource = typeof(TSource).GetTypeDetail();
        //    var typeTarget = typeof(TTarget).GetTypeDetail();

        //    var sourceParameterExpression = Expression.Parameter(typeSource.Type, "x");

        //    var selectorExpression = GenerateSelectorExpression(typeSource, typeTarget, sourceParameterExpression, graph, new Stack<Type>());

        //    var lambda = Expression.Lambda<Func<TSource, TTarget>>(selectorExpression, sourceParameterExpression);
        //    return lambda;
        //}
        //private static Expression GenerateSelectorExpression(TypeDetail typeSource, TypeDetail typeTarget, Expression sourceExpression, Graph? graph, Stack<Type> stack)
        //{
        //    stack.Push(typeSource.Type);

        //    var bindings = new List<MemberBinding>();

        //    foreach (var targetProperty in typeTarget.MemberDetails)
        //    {
        //        if (typeSource.TryGetMember(targetProperty.Name, out var sourceProperty))
        //        {
        //            if (graph is not null && !graph.HasMember(targetProperty.Name))
        //                continue;

        //            if (sourceProperty.TypeDetail.CoreType.HasValue)
        //            {
        //                //Basic Property

        //                Expression sourcePropertyExpression;
        //                if (sourceProperty.MemberInfo is PropertyInfo propertyInfo)
        //                    sourcePropertyExpression = Expression.Property(sourceExpression, propertyInfo);
        //                else if (sourceProperty.MemberInfo is FieldInfo fieldInfo)
        //                    sourcePropertyExpression = Expression.Field(sourceExpression, fieldInfo);
        //                else
        //                    throw new InvalidOperationException();

        //                MemberBinding binding = Expression.Bind(targetProperty.MemberInfo, sourcePropertyExpression);
        //                bindings.Add(binding);
        //            }
        //            else if (sourceProperty.Type.IsArray || sourceProperty.TypeDetail.IsIEnumerableGeneric)
        //            {
        //                //Related Enumerable
        //                if (!targetProperty.Type.IsArray && !targetProperty.TypeDetail.IsIEnumerableGeneric)
        //                    continue;
        //                var childGraph = graph?.GetChildGraph(targetProperty.Name);

        //                var sourcePropertyGenericType = sourceProperty.TypeDetail.InnerTypeDetail;
        //                if (stack.Where(x => x == sourcePropertyGenericType.Type).Count() >= maxRecursiveDepth)
        //                    continue;

        //                var targetPropertyGenericType = targetProperty.TypeDetail.InnerTypeDetail;
        //                Expression sourcePropertyExpression;
        //                if (sourceProperty.MemberInfo is PropertyInfo propertyInfo)
        //                    sourcePropertyExpression = Expression.Property(sourceExpression, propertyInfo);
        //                else if (sourceProperty.MemberInfo is FieldInfo fieldInfo)
        //                    sourcePropertyExpression = Expression.Field(sourceExpression, fieldInfo);
        //                else
        //                    throw new InvalidOperationException();

        //                var sourcePropertyParameterExpression = Expression.Parameter(sourcePropertyGenericType.Type, "y");
        //                var mapperExpression = GenerateSelectorExpression(sourcePropertyGenericType, targetPropertyGenericType, sourcePropertyParameterExpression, childGraph, stack);
        //                Expression sourcePropertyLambda = Expression.Lambda(mapperExpression, [sourcePropertyParameterExpression]);

        //                var selectMethodGeneric = selectMethod.MakeGenericMethod(sourcePropertyGenericType.Type, targetPropertyGenericType.Type);
        //                var callSelect = Expression.Call(selectMethodGeneric, sourcePropertyExpression, sourcePropertyLambda);

        //                Expression toExpression;
        //                if (targetProperty.Type.IsArray)
        //                {
        //                    toExpression = Expression.Call(arrayMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                }
        //                else if (targetProperty.TypeDetail.IsIListGeneric || targetProperty.Type.Name == "List`1")
        //                {
        //                    toExpression = Expression.Call(arrayMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                }
        //                else if (targetProperty.TypeDetail.IsISetGeneric || targetProperty.Type.Name == "HashSet`1")
        //                {
        //                    toExpression = Expression.Call(hashSetMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                }
        //                else if (targetProperty.TypeDetail.IsICollectionGeneric)
        //                {
        //                    toExpression = Expression.Call(arrayMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                }
        //                else
        //                {
        //                    throw new NotSupportedException($"Graph {nameof(GenerateSelect)} does not support type {targetProperty.Type.GetNiceName()}");
        //                }

        //                MemberBinding binding = Expression.Bind(targetProperty.MemberInfo, toExpression);
        //                bindings.Add(binding);
        //            }
        //            else
        //            {
        //                //Related Single
        //                var childGraph = graph?.GetChildGraph(targetProperty.Name);

        //                if (stack.Where(x => x == sourceProperty.Type).Count() >= maxRecursiveDepth)
        //                    continue;

        //                Expression sourcePropertyExpression;
        //                if (sourceProperty.MemberInfo is PropertyInfo propertyInfo)
        //                    sourcePropertyExpression = Expression.Property(sourceExpression, propertyInfo);
        //                else if (sourceProperty.MemberInfo is FieldInfo fieldInfo)
        //                    sourcePropertyExpression = Expression.Field(sourceExpression, fieldInfo);
        //                else
        //                    throw new InvalidOperationException();

        //                Expression sourceNullIf = Expression.Equal(sourcePropertyExpression, Expression.Constant(null));
        //                var mapperExpression = GenerateSelectorExpression(sourceProperty.TypeDetail, targetProperty.TypeDetail, sourcePropertyExpression, childGraph, stack);
        //                Expression sourcePropertyConditionalExpression = Expression.Condition(sourceNullIf, Expression.Convert(Expression.Constant(null), targetProperty.Type), mapperExpression, targetProperty.Type);
        //                MemberBinding binding = Expression.Bind(targetProperty.MemberInfo, sourcePropertyConditionalExpression);
        //                bindings.Add(binding);
        //            }
        //        }
        //    }

        //    _ = stack.Pop();

        //    var initializer = Expression.MemberInit(Expression.New(typeTarget.Type), bindings);
        //    return initializer;
        //}
    }
}