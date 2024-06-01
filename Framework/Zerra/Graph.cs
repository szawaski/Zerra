// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zerra.Collections;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra
{
    public class Graph
    {
        protected string? name;
        protected string? type;

        protected bool includeAllProperties;
        protected readonly HashSet<string> localProperties;
        protected readonly HashSet<string> removedProperties;
        protected readonly Dictionary<string, Graph> childGraphs;

        public bool IncludeAllProperties => includeAllProperties;
        public IEnumerable<string> LocalProperties
        {
            get
            {
                if (includeAllProperties)
                {
                    var type = GetModelType();
                    if (type == null)
                        throw new Exception($"{nameof(Graph)} has no type information so cannot enumerate {nameof(LocalProperties)} with {nameof(IncludeAllProperties)}");
                    return type.GetTypeDetail().MemberDetails.Where(x => !removedProperties.Contains(x.Name) && !childGraphs.ContainsKey(x.Name)).Select(x => x.Name);
                }
                else
                {
                    return localProperties;
                }
            }
        }

        public IReadOnlyCollection<Graph> ChildGraphs => childGraphs.Values;

        [NonSerialized]
        protected string? signature = null;
        public string Signature
        {
            get
            {
                if (this.signature == null)
                {
                    GenerateSignature();
                }
                return this.signature!;
            }
        }

        public Graph(Graph graph)
        {
            if (graph != null)
            {
                this.name = graph.name;
                this.includeAllProperties = graph.includeAllProperties;
                this.localProperties = new(graph.localProperties);
                this.removedProperties = new(graph.removedProperties);
                this.childGraphs = new(graph.childGraphs);
                this.type = graph.type;
                this.signature = graph.signature;
            }
            else
            {
                this.includeAllProperties = true;
                this.localProperties = new();
                this.removedProperties = new();
                this.childGraphs = new();
                this.type = null;
                this.signature = null;
            }
        }

        public Graph() : this(null, false, null, (IReadOnlyCollection<Graph>?)null)
        {
            this.signature = "";
        }
        public Graph(bool includeAllProperties) : this(null, includeAllProperties, null, (IReadOnlyCollection<Graph>?)null) { }
        public Graph(params string[] properties) : this(null, false, properties, null) { }
        public Graph(bool includeAllProperties, params string[]? properties) : this(null, includeAllProperties, properties, null) { }
        public Graph(string? name, bool includeAllProperties, params string[]? properties) : this(name, includeAllProperties, properties, null) { }

        public Graph(IReadOnlyCollection<string>? properties) : this(null, false, properties, null) { }
        public Graph(bool includeAllProperties, IReadOnlyCollection<string>? properties) : this(null, includeAllProperties, properties, null) { }
        public Graph(bool includeAllProperties, IReadOnlyCollection<string>? properties, IReadOnlyCollection<Graph>? childGraphs) : this(null, includeAllProperties, properties, childGraphs) { }
        public Graph(string? name, IReadOnlyCollection<string> properties) : this(name, false, properties, null) { }
        public Graph(string? name, bool includeAllProperties, IReadOnlyCollection<string>? properties) : this(name, includeAllProperties, properties, null) { }
        public Graph(string? name, bool includeAllProperties, IReadOnlyCollection<string>? properties, IReadOnlyCollection<Graph>? childGraphs)
        {
            this.name = name;
            this.includeAllProperties = includeAllProperties;
            this.localProperties = new();
            removedProperties = new();
            this.childGraphs = new();

            if (properties != null)
                AddProperties(properties);

            if (childGraphs != null)
                AddChildGraphs(childGraphs);
        }

        public bool IsEmpty => !includeAllProperties && localProperties.Count == 0 && childGraphs.Count == 0;

        public override bool Equals(object? obj)
        {
            if (obj is not Graph objCasted)
                return false;
            return this.Signature == objCasted.Signature;
        }
        public override int GetHashCode()
        {
            return this.Signature.GetHashCode();
        }

        protected void GenerateSignature()
        {
            var writer = new CharWriter();
            try
            {
                GenerateSignatureBuilder(ref writer);
                this.signature = writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        private void GenerateSignatureBuilder(ref CharWriter writer)
        {
            if (!String.IsNullOrEmpty(this.name))
            {
                writer.Write("N:");
                writer.Write(this.name);
            }

            if (this.includeAllProperties)
                writer.Write("A");

            foreach (var property in this.localProperties.OrderBy(x => x))
            {
                writer.Write("P:");
                writer.Write(property);
            }

            if (includeAllProperties)
            {
                foreach (var property in this.removedProperties.OrderBy(x => x))
                {
                    writer.Write("R:");
                    writer.Write(property);
                }
            }

            foreach (var graph in this.childGraphs.Values.OrderBy(x => x.name))
            {
                writer.Write("G:");
                writer.Write("(");
                graph.GenerateSignatureBuilder(ref writer);
                writer.Write(")");
            }
        }

        public void AddProperties(params string[] properties) { AddProperties((IEnumerable<string>)properties); }
        public void AddProperties(IEnumerable<string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            foreach (var property in properties)
                AddProperty(property);
        }

        public void RemoveProperties(params string[] properties) { RemoveProperties((IEnumerable<string>)properties); }
        public void RemoveProperties(IEnumerable<string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            foreach (var property in properties)
                RemoveProperty(property);
        }

        public void AddAllProperties()
        {
            if (!includeAllProperties)
            {
                includeAllProperties = true;
                this.signature = null;
            }
        }
        public void RemoveAllProperties()
        {
            if (includeAllProperties)
            {
                includeAllProperties = false;
                this.signature = null;
            }
        }

        public void AddChildGraphs(params Graph[] graphs) { AddChildGraphs((IEnumerable<Graph>)graphs); }
        public void AddChildGraphs(IEnumerable<Graph> graphs)
        {
            if (graphs == null)
                throw new ArgumentNullException(nameof(graphs));

            foreach (var graph in graphs)
                AddChildGraph(graph);
            this.signature = null;
        }
        public void RemoveChildGraphs(params Graph[] graphs) { RemoveChildGraphs((IEnumerable<Graph>)graphs); }
        public void RemoveChildGraphs(IEnumerable<Graph> graphs)
        {
            if (graphs == null)
                throw new ArgumentNullException(nameof(graphs));

            foreach (var graph in graphs)
                RemoveChildGraph(graph);
            this.signature = null;
        }

        public void ReplaceChildGraphs(params Graph?[]? graphs)
        {
            if (graphs == null || graphs.Length == 0)
                return;

            foreach (var graph in graphs)
            {
                if (graph == null)
                    continue;

                if (String.IsNullOrWhiteSpace(graph.name))
                    throw new InvalidOperationException("Cannot add a graph without a property name.");

                if (childGraphs.ContainsKey(graph.name))
                    _ = childGraphs.Remove(graph.name);

                childGraphs.Add(graph.name, graph);
            }
            this.signature = null;
        }
        public void Clear()
        {
            localProperties.Clear();
            removedProperties.Clear();
            childGraphs.Clear();
            signature = null;
        }

        private void AddProperty(string property)
        {
            if (childGraphs.TryGetValue(property, out var childGraph))
            {
                childGraph.includeAllProperties = true;
            }
            else
            {
                _ = this.localProperties.Add(property);
                _ = this.removedProperties.Remove(property);
            }
        }
        private void RemoveProperty(string property)
        {
            _ = this.localProperties.Remove(property);
            _ = removedProperties.Add(property);
            if (childGraphs.ContainsKey(property))
                _ = childGraphs.Remove(property);
        }
        private void AddChildGraph(Graph graph)
        {
            if (String.IsNullOrWhiteSpace(graph.name))
                throw new InvalidOperationException("Cannot add a child graph without a name.");

            if (childGraphs.TryGetValue(graph.name, out var childGraph) && childGraph.name != null)
            {
                childGraph.includeAllProperties |= graph.includeAllProperties;
                childGraph.AddProperties(graph.localProperties);

                if (this.localProperties.Contains(childGraph.name))
                {
                    childGraph.includeAllProperties = true;
                    _ = this.localProperties.Remove(childGraph.name);
                }
            }
            else
            {
                graph.SetModelTypeToChildGraphs(this);
                childGraphs.Add(graph.name, graph);
            }
        }
        private void RemoveChildGraph(Graph graph)
        {
            if (String.IsNullOrWhiteSpace(graph.name))
                throw new InvalidOperationException("Cannot remove a child graph without a name.");

            _ = this.localProperties.Remove(graph.name);
            _ = removedProperties.Add(graph.name);
            if (childGraphs.ContainsKey(graph.name))
                _ = childGraphs.Remove(graph.name);
        }
        protected void AddMembers(Stack<MemberInfo> members)
        {
            if (members.Count == 0)
                return;

            var member = members.Pop();
            if (members.Count == 0)
            {
                AddProperty(member.Name);
            }
            else
            {
                if (!childGraphs.TryGetValue(member.Name, out var childGraph))
                {
                    childGraph = new Graph();
                    childGraph.name = member.Name;
                    if (member.MemberType == MemberTypes.Property)
                        childGraph.type = ((PropertyInfo)member).PropertyType.FullName;
                    else
                        childGraph.type = ((FieldInfo)member).FieldType.FullName;
                    childGraphs.Add(member.Name, childGraph);
                }
                if (childGraph.name != null && this.localProperties.Contains(childGraph.name))
                {
                    childGraph.includeAllProperties = true;
                    _ = this.localProperties.Remove(childGraph.name);
                }
                childGraph.AddMembers(members);
            }
        }
        protected void RemoveMembers(Stack<MemberInfo> members)
        {
            if (members.Count == 0)
                return;

            var member = members.Pop();
            if (members.Count == 0)
            {
                RemoveProperty(member.Name);
            }
            else
            {
                if (childGraphs.TryGetValue(member.Name, out var childGraph))
                    childGraph.RemoveMembers(members);
            }
        }

        public Graph<T> Convert<T>()
            where T : class
        {
            return new Graph<T>(this);
        }

        public Graph Convert(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return Convert(this, type);
        }

        public bool HasLocalProperty(string name)
        {
            return (this.includeAllProperties && !this.removedProperties.Contains(name) && !childGraphs.ContainsKey(name)) || this.localProperties.Contains(name);
        }
        public bool HasProperty(string name)
        {
            return (this.includeAllProperties && !this.removedProperties.Contains(name)) || this.localProperties.Contains(name) || childGraphs.ContainsKey(name);
        }
        public bool HasChild(string name)
        {
            return this.localProperties.Contains(name) || childGraphs.ContainsKey(name);
        }

        public Graph? GetChildGraph(string name)
        {
            if (childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var childGraph))
                return null;
            return childGraph;
        }
        public Graph? GetChildGraph(string name, Type type)
        {
            if (childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var nonGenericGraph))
                return null;
            return Convert(nonGenericGraph, type);
        }
        public Graph<T>? GetChildGraph<T>(string name)
        {
            if (childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var nonGenericGraph))
                return null;
            return new Graph<T>(nonGenericGraph);
        }

        public static Graph Convert(Graph graph, Type type)
        {
            var constructor = TypeAnalyzer.GetGenericTypeDetail(typeof(Graph<>), type).GetConstructor(new Type[] { typeof(Graph) });
            var genericGraph = (Graph)constructor.CreatorBoxed(new object[] { graph });
            return genericGraph;
        }

        public static Graph Empty()
        {
            return new Graph();
        }

        public Graph Copy()
        {
            return new Graph(this);
        }

        public override string ToString()
        {
            var writer = new CharWriter();
            try
            {
                ToString(ref writer, 0);
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        private void ToString(ref CharWriter writer, int depth)
        {
            foreach (var property in this.LocalProperties)
            {
                for (var i = 0; i < depth * 3; i++)
                    writer.Write(' ');

                writer.Write(property);
                writer.Write(Environment.NewLine);
            }
            foreach (var childGraph in this.childGraphs.Values)
            {
                if (String.IsNullOrEmpty(childGraph.name))
                    continue;

                for (var i = 0; i < depth * 3; i++)
                    writer.Write(' ');

                writer.Write(childGraph.name);
                writer.Write(Environment.NewLine);
                childGraph.ToString(ref writer, depth + 1);

            }
        }

        public virtual Type? GetModelType()
        {
            if (!String.IsNullOrWhiteSpace(type))
            {
                return Discovery.GetTypeFromName(type);
            }
            return null;
        }

        public void SetModelType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            this.type = type.FullName;
            foreach (var childGraph in childGraphs.Values)
            {
                childGraph.SetModelTypeToChildGraphs(this);
            }
        }
        private void SetModelTypeToChildGraphs(Graph parent)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Cannot update child graphs without a name.");

            type = null;
            var modelType = parent.GetModelType();
            if (modelType != null)
            {
                var modelInfo = TypeAnalyzer.GetTypeDetail(modelType);
                if (modelInfo.TryGetMember(name, out var property))
                {
                    var subModelType = property.Type;
                    var subModelTypeDetails = TypeAnalyzer.GetTypeDetail(subModelType);
                    if (subModelTypeDetails.HasIEnumerableGeneric)
                    {
                        subModelType = subModelTypeDetails.IEnumerableGenericInnerType;
                    }
                    type = subModelType.AssemblyQualifiedName;
                    foreach (var childGraph in childGraphs.Values)
                    {
                        childGraph.SetModelTypeToChildGraphs(this);
                    }
                }
            }
        }

        public void MergePropertiesForIdenticalModelTypes()
        {
            var modelTypesWithGraphs = new Dictionary<string, List<Graph>>();
            ListModelTypesWithGraphs(modelTypesWithGraphs);

            foreach (var graphs in modelTypesWithGraphs)
            {
                foreach (var g in graphs.Value)
                {
                    g.MergePropertiesForIdenticalModelTypesRecursionCheck(graphs.Key);
                }
            }

            foreach (var graphs in modelTypesWithGraphs)
            {
                var allProperties = graphs.Value.SelectMany(x => x.localProperties).ToArray();
                var allChildGraphs = graphs.Value.SelectMany(x => x.childGraphs.Values).ToArray();
                graphs.Value.ForEach(x => x.AddProperties(allProperties));
                graphs.Value.ForEach(x => x.AddChildGraphs(allChildGraphs));
            }
        }
        private void MergePropertiesForIdenticalModelTypesRecursionCheck(string modelTypeName)
        {
            foreach (var childGraph in childGraphs.Values)
            {
                if (childGraph.type == modelTypeName)
                {
                    throw new Exception($"Graph has a recursive model type for {modelTypeName}. Cannot merge properties.");
                }
                childGraph.MergePropertiesForIdenticalModelTypesRecursionCheck(modelTypeName);
            }
        }
        private void ListModelTypesWithGraphs(Dictionary<string, List<Graph>> results)
        {
            if (!String.IsNullOrWhiteSpace(type))
            {
                if (!results.ContainsKey(type))
                    results.Add(type, new List<Graph>());
                results[type].Add(this);
            }

            foreach (var childGraph in childGraphs.Values.Where(x => !String.IsNullOrWhiteSpace(x.type)))
            {
                childGraph.ListModelTypesWithGraphs(results);
            }
        }

        private static readonly byte maxSelectRecursive = 2;
        private static readonly MethodInfo selectMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "Select" && m.GetParameters().Length == 2).First();
        private static readonly MethodInfo listMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToList").First();
        private static readonly ConcurrentFactoryDictionary<TypeKey, Expression> selectExpressions = new();
        public Expression<Func<TSource, TTarget>> GenerateSelect<TSource, TTarget>()
        {
            var key = new TypeKey(this.Signature, typeof(TSource), typeof(TTarget));

            var expression = (Expression<Func<TSource, TTarget>>)selectExpressions.GetOrAdd(key, (_) =>
            {
                return GenerateSelectorExpression<TSource, TTarget>(this);
            });

            return expression;
        }
        public static Expression<Func<TSource, TTarget>> GenerateSelect<TSource, TTarget>(Graph? graph)
        {
            var key = new TypeKey(graph?.Signature, typeof(TSource), typeof(TTarget));

            var expression = (Expression<Func<TSource, TTarget>>)selectExpressions.GetOrAdd(key, (_) =>
            {
                return GenerateSelectorExpression<TSource, TTarget>(graph);
            });

            return expression;
        }
        private static Expression<Func<TSource, TTarget>> GenerateSelectorExpression<TSource, TTarget>(Graph? graph)
        {
            var typeSource = typeof(TSource);
            var typeTarget = typeof(TTarget);

            var sourceParameterExpression = Expression.Parameter(typeSource, "x");

            var selectorExpression = GenerateSelectorExpression(typeSource, typeTarget, sourceParameterExpression, graph, new Stack<Type>());

            var lambda = Expression.Lambda<Func<TSource, TTarget>>(selectorExpression, sourceParameterExpression);
            return lambda;
        }
        private static Expression GenerateSelectorExpression(Type typeSource, Type typeTarget, Expression sourceExpression, Graph? graph, Stack<Type> stack)
        {
            var sourceProperties = typeSource.GetProperties();
            var targetProperites = typeTarget.GetProperties();

            stack.Push(typeSource);

            var bindings = new List<MemberBinding>();

            foreach (var targetProperty in targetProperites.Where(x => x.CanWrite))
            {
                var sourceProperty = sourceProperties.FirstOrDefault(x => x.CanRead && x.Name == targetProperty.Name);

                if (sourceProperty != null)
                {
                    if (targetProperty.PropertyType == sourceProperty.PropertyType)
                    {
                        //Basic Property
                        if (graph == null || graph.HasLocalProperty(targetProperty.Name))
                        {
                            Expression sourcePropertyExpression = Expression.Property(sourceExpression, sourceProperty);

                            MemberBinding binding = Expression.Bind(targetProperty, sourcePropertyExpression);
                            bindings.Add(binding);
                        }
                    }
                    else if (sourceProperty.PropertyType.GetInterface(typeof(IEnumerable<>).MakeGenericType(targetProperty.PropertyType).Name) != null)
                    {
                        //Related Enumerable
                        var childGraph = graph?.GetChildGraph(targetProperty.Name);
                        if (childGraph != null)
                        {
                            if (targetProperty.PropertyType.IsGenericType)
                            {
                                var sourcePropertyGenericType = sourceProperty.PropertyType.GetGenericArguments()[0];
                                if (stack.Where(x => x == sourcePropertyGenericType).Count() < maxSelectRecursive)
                                {
                                    var targetPropertyGenericType = targetProperty.PropertyType.GetGenericArguments()[0];
                                    Expression sourcePropertyExpression = Expression.Property(sourceExpression, sourceProperty);

                                    var sourcePropertyParameterExpression = Expression.Parameter(sourcePropertyGenericType, "y");
                                    var mapperExpression = GenerateSelectorExpression(sourcePropertyGenericType, targetPropertyGenericType, sourcePropertyParameterExpression, childGraph, stack);
                                    Expression sourcePropertyLambda = Expression.Lambda(mapperExpression, new ParameterExpression[] { sourcePropertyParameterExpression });

                                    var selectMethodGeneric = selectMethod.MakeGenericMethod(sourcePropertyGenericType, targetPropertyGenericType);
                                    var callSelect = Expression.Call(selectMethodGeneric, sourcePropertyExpression, sourcePropertyLambda);

                                    var listMethodGeneric = listMethod.MakeGenericMethod(targetPropertyGenericType);
                                    var listSelect = Expression.Call(listMethodGeneric, callSelect);

                                    MemberBinding binding = Expression.Bind(targetProperty, listSelect);
                                    bindings.Add(binding);
                                }
                            }
                        }
                    }
                    else
                    {
                        //Related Single
                        var childGraph = graph?.GetChildGraph(targetProperty.Name);
                        if (childGraph != null)
                        {
                            if (stack.Where(x => x == sourceProperty.PropertyType).Count() < maxSelectRecursive)
                            {
                                Expression sourcePropertyExpression = Expression.Property(sourceExpression, sourceProperty);
                                Expression sourceNullIf = Expression.Equal(sourcePropertyExpression, Expression.Constant(null));
                                var mapperExpression = GenerateSelectorExpression(sourceProperty.PropertyType, targetProperty.PropertyType, sourcePropertyExpression, childGraph, stack);
                                Expression sourcePropertyConditionalExpression = Expression.Condition(sourceNullIf, Expression.Convert(Expression.Constant(null), targetProperty.PropertyType), mapperExpression, targetProperty.PropertyType);
                                MemberBinding binding = Expression.Bind(targetProperty, sourcePropertyConditionalExpression);
                                bindings.Add(binding);
                            }
                        }
                    }
                }
            }

            _ = stack.Pop();

            var initializer = Expression.MemberInit(Expression.New(typeTarget), bindings);
            return initializer;
        }
    }
}