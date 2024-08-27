// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Zerra.Collections;
using Zerra.Reflection;

namespace Zerra
{
    public class Graph
    {
        protected string? name;
        protected string? type;

        protected bool includeAllProperties;
        protected HashSet<string>? localProperties;
        protected HashSet<string>? removedProperties;
        protected Dictionary<string, Graph>? childGraphs;
        protected Dictionary<object, Graph>? instanceGraphs;

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
                    return type.GetTypeDetail().MemberDetails.Where(x => (removedProperties is null || !removedProperties.Contains(x.Name)) && (childGraphs is null || !childGraphs.ContainsKey(x.Name))).Select(x => x.Name);
                }
                else
                {
                    if (localProperties is null)
                        return Array.Empty<string>();
                    return localProperties;
                }
            }
        }

        public IReadOnlyCollection<Graph> ChildGraphs => childGraphs is null ? Array.Empty<Graph>() : childGraphs.Values;

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

        public Graph(Graph? graph)
        {
            if (graph is not null)
            {
                this.name = graph.name;
                this.includeAllProperties = graph.includeAllProperties;
                if (graph.localProperties is not null)
                    this.localProperties = new(graph.localProperties);
                if (graph.removedProperties is not null)
                    this.removedProperties = new(graph.removedProperties);
                if (graph.childGraphs is not null)
                    this.childGraphs = new(graph.childGraphs);
                this.type = graph.type;
                this.signature = graph.signature;
            }
            else
            {
                this.signature = "";
            }
        }

        public Graph()
        {
            this.signature = "";
        }
        public Graph(bool includeAllProperties)
        {
            this.includeAllProperties = true;
            this.signature = "A";
        }
        public Graph(params string[] properties) : this(null, false, properties, null) { }
        public Graph(bool includeAllProperties, params string[]? properties) : this(null, includeAllProperties, properties, null) { }
        public Graph(string? name, bool includeAllProperties, params string[]? properties) : this(name, includeAllProperties, properties, null) { }

        public Graph(IEnumerable<string>? properties) : this(null, false, properties, null) { }
        public Graph(IEnumerable<string>? properties, IEnumerable<Graph>? childGraphs) : this(null, false, properties, childGraphs) { }
        public Graph(bool includeAllProperties, IEnumerable<string>? properties) : this(null, includeAllProperties, properties, null) { }
        public Graph(bool includeAllProperties, IEnumerable<string>? properties, IEnumerable<Graph>? childGraphs) : this(null, includeAllProperties, properties, childGraphs) { }
        public Graph(string? name, IEnumerable<string> properties) : this(name, false, properties, null) { }
        public Graph(string? name, bool includeAllProperties, IEnumerable<string>? properties) : this(name, includeAllProperties, properties, null) { }
        public Graph(string? name, bool includeAllProperties, IEnumerable<string>? properties, IEnumerable<Graph>? childGraphs)
        {
            this.name = name;
            this.includeAllProperties = includeAllProperties;

            if (properties != null)
                AddProperties(properties);

            if (childGraphs != null)
            {
                foreach (var graph in childGraphs)
                    AddChildGraph(graph);
            }
        }

        public bool IsEmpty => !includeAllProperties && (localProperties?.Count ?? 0) == 0 && (childGraphs?.Count ?? 0) == 0;

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
        public static bool operator ==(Graph? graph1, Graph? graph2)
        {
            if (graph1 is null)
                return graph2 is null;
            if (graph2 is null)
                return false;
            return graph1.Signature == graph2.Signature;
        }
        public static bool operator !=(Graph? graph1, Graph? graph2)
        {
            if (graph1 is null)
                return graph2 is not null;
            if (graph2 is null)
                return true;
            return graph1.Signature != graph2.Signature;
        }

        protected void GenerateSignature()
        {
            var sb = new StringBuilder();
            GenerateSignatureBuilder(sb);
            signature = sb.ToString();
        }
        private void GenerateSignatureBuilder(StringBuilder sb)
        {
            if (instanceGraphs is not null)
                throw new InvalidOperationException("Graphs with instances cannot be compared so cannot be used here");

            if (!String.IsNullOrEmpty(name))
            {
                _ = sb.Append("N:");
                _ = sb.Append(name);
            }

            if (includeAllProperties)
                _ = sb.Append("A:");

            if (localProperties is not null)
            {
                foreach (var property in localProperties.OrderBy(x => x))
                {
                    _ = sb.Append("P:");
                    _ = sb.Append(property);
                }
            }

            if (includeAllProperties && removedProperties is not null)
            {
                foreach (var property in removedProperties.OrderBy(x => x))
                {
                    _ = sb.Append("R:");
                    _ = sb.Append(property);
                }
            }

            if (childGraphs is not null)
            {
                foreach (var graph in childGraphs.Values.OrderBy(x => x.name))
                {
                    _ = sb.Append("G:(");
                    graph.GenerateSignatureBuilder(sb);
                    _ = sb.Append(")");
                }
            }
        }

        public void AddProperties(params string[] properties) => AddProperties((IEnumerable<string>)properties);
        public void AddProperties(IEnumerable<string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            foreach (var property in properties)
                AddProperty(property);
        }

        public void RemoveProperties(params string[] properties) => RemoveProperties((IEnumerable<string>)properties);
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
                signature = null;
            }
        }
        public void RemoveAllProperties()
        {
            if (includeAllProperties)
            {
                includeAllProperties = false;
                signature = null;
            }
        }

        public void AddProperty(string property)
        {
            if (String.IsNullOrWhiteSpace(property))
                throw new ArgumentNullException(nameof(property));

            if (childGraphs is not null && childGraphs.TryGetValue(property, out var childGraph))
            {
                childGraph.includeAllProperties = true;
            }
            else
            {
                localProperties ??= new();
                _ = localProperties.Add(property);
                _ = removedProperties?.Remove(property);
            }
            signature = null;

        }
        public void RemoveProperty(string property)
        {
            if (String.IsNullOrWhiteSpace(property))
                throw new ArgumentNullException(nameof(property));

            _ = localProperties?.Remove(property);
            _ = removedProperties?.Add(property);
            _ = childGraphs?.Remove(property);

            signature = null;
        }
        public void ReplaceChildGraph(Graph graph)
        {
            if (String.IsNullOrWhiteSpace(graph.name))
                throw new InvalidOperationException("Cannot add a graph without a property name.");

            _ = childGraphs?.Remove(graph.name);
            childGraphs?.Add(graph.name, graph);

            signature = null;
        }
        public void AddChildGraph(Graph graph)
        {
            if (String.IsNullOrWhiteSpace(graph.name))
                throw new InvalidOperationException("Cannot add a child graph without a name.");

            childGraphs ??= new();
            if (childGraphs.TryGetValue(graph.name, out var childGraph) && childGraph.name != null)
            {
                childGraph.includeAllProperties |= graph.includeAllProperties;
                if (graph.localProperties is not null)
                    childGraph.AddProperties(graph.localProperties);

                if (localProperties is not null && localProperties.Contains(childGraph.name))
                {
                    childGraph.includeAllProperties = true;
                    _ = localProperties.Remove(childGraph.name);
                }
            }
            else
            {
                graph.SetModelTypeToChildGraphs(this);
                childGraphs.Add(graph.name, graph);
            }

            signature = null;
        }
        public void RemoveChildGraph(Graph graph)
        {
            if (String.IsNullOrWhiteSpace(graph.name))
                throw new InvalidOperationException("Cannot remove a child graph without a name.");

            _ = localProperties?.Remove(graph.name);
            _ = removedProperties?.Add(graph.name);
            _ = childGraphs?.Remove(graph.name);

            signature = null;
        }
        public void AddInstanceGraph(object instance, Graph graph)
        {
            instanceGraphs ??= new();
            instanceGraphs[instance] = graph;
            signature = null;
        }
        public void RemoveInstanceGraph(object instance)
        {
            if (instanceGraphs is null)
                return;
            _ = instanceGraphs.Remove(instance);
            signature = null;
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
                childGraphs ??= new();
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
                if (childGraph.name != null && localProperties is not null && localProperties.Contains(childGraph.name))
                {
                    childGraph.includeAllProperties = true;
                    _ = localProperties.Remove(childGraph.name);
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
                if (childGraphs is not null && childGraphs.TryGetValue(member.Name, out var childGraph))
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
            return (includeAllProperties && (removedProperties is null || !removedProperties.Contains(name)) && (childGraphs is null || !childGraphs.ContainsKey(name))) || (localProperties is not null && localProperties.Contains(name));
        }
        public bool HasProperty(string name)
        {
            return (includeAllProperties && (removedProperties is null || !removedProperties.Contains(name))) || (localProperties is not null && localProperties.Contains(name)) || (childGraphs is not null && childGraphs.ContainsKey(name));
        }
        public bool HasChild(string name)
        {
            return (localProperties is not null && localProperties.Contains(name)) || (childGraphs is not null && childGraphs.ContainsKey(name));
        }

        public Graph? GetChildGraph(string name)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var childGraph))
                return null;
            return childGraph;
        }
        public Graph? GetChildGraph(string name, Type type)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var nonGenericGraph))
                return null;
            if (nonGenericGraph.GetModelType() == type)
                return nonGenericGraph;
            return Convert(nonGenericGraph, type);
        }
        public Graph<T>? GetChildGraph<T>(string name)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var nonGenericGraph))
                return null;
            if (nonGenericGraph.GetModelType() == typeof(T))
                return (Graph<T>)nonGenericGraph;
            return new Graph<T>(nonGenericGraph);
        }

        public Graph GetInstanceGraph(object instance)
        {
            if (instanceGraphs is not null && instanceGraphs.TryGetValue(instance, out var instanceGraph))
                return instanceGraph;
            return this;
        }
        public Graph? GetChildInstanceGraph(string name, object instance)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var childGraph))
                return null;

            if (childGraph.instanceGraphs is not null && childGraph.instanceGraphs.TryGetValue(instance, out var instanceGraph))
                return instanceGraph;
            return childGraph;
        }
        public Graph? GetChildInstanceGraph(string name, Type type, object instance)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var nonGenericGraph))
                return null;
            if (nonGenericGraph.GetModelType() == type)
                return nonGenericGraph;

            if (nonGenericGraph.instanceGraphs is not null && nonGenericGraph.instanceGraphs.TryGetValue(instance, out var instanceGraph))
                return Convert(instanceGraph, type);
            return Convert(nonGenericGraph, type);
        }
        public Graph<T>? GetChildInstanceGraph<T>(string name, object instance)
        {
            if (childGraphs is null || childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out var nonGenericGraph))
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

        public static Graph Convert(Graph graph, Type type)
        {
            var constructor = TypeAnalyzer.GetGenericTypeDetail(typeof(Graph<>), type).GetConstructor([typeof(Graph)]);
            var genericGraph = (Graph)constructor.CreatorBoxed([graph]);
            return genericGraph;
        }

        public Graph Copy()
        {
            return new Graph(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb, 0);
            return sb.ToString();
        }
        private void ToString(StringBuilder sb, int depth)
        {
            foreach (var property in LocalProperties)
            {
                if (sb.Length > 0)
                    _ = sb.Append(Environment.NewLine);
                for (var i = 0; i < depth; i++)
                    _ = sb.Append("  ");

                _ = sb.Append(property);
            }
            if (childGraphs is not null)
            {
                foreach (var childGraph in childGraphs.Values)
                {
                    if (sb.Length > 0)
                        _ = sb.Append(Environment.NewLine);
                    if (String.IsNullOrEmpty(childGraph.name))
                        continue;

                    for (var i = 0; i < depth; i++)
                        _ = sb.Append("  ");

                    _ = sb.Append(childGraph.name);
                    childGraph.ToString(sb, depth + 1);
                }
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
            if (childGraphs is not null)
            {
                foreach (var childGraph in childGraphs.Values)
                {
                    childGraph.SetModelTypeToChildGraphs(this);
                }
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
                    if (childGraphs is not null)
                    {
                        foreach (var childGraph in childGraphs.Values)
                        {
                            childGraph.SetModelTypeToChildGraphs(this);
                        }
                    }
                }
            }
        }

        //public void MergePropertiesForIdenticalModelTypes()
        //{
        //    var modelTypesWithGraphs = new Dictionary<string, List<Graph>>();
        //    ListModelTypesWithGraphs(modelTypesWithGraphs);

        //    foreach (var graphs in modelTypesWithGraphs)
        //    {
        //        foreach (var g in graphs.Value)
        //        {
        //            g.MergePropertiesForIdenticalModelTypesRecursionCheck(graphs.Key);
        //        }
        //    }

        //    foreach (var graphs in modelTypesWithGraphs)
        //    {
        //        var allProperties = graphs.Value.SelectMany(x => x.localProperties).ToArray();
        //        var allChildGraphs = graphs.Value.SelectMany(x => x.childGraphs.Values).ToArray();
        //        graphs.Value.ForEach(x => x.AddProperties(allProperties));
        //        graphs.Value.ForEach(x => x.AddChildGraphs(allChildGraphs));
        //    }
        //}
        //private void MergePropertiesForIdenticalModelTypesRecursionCheck(string modelTypeName)
        //{
        //    foreach (var childGraph in childGraphs.Values)
        //    {
        //        if (childGraph.type == modelTypeName)
        //        {
        //            throw new Exception($"Graph has a recursive model type for {modelTypeName}. Cannot merge properties.");
        //        }
        //        childGraph.MergePropertiesForIdenticalModelTypesRecursionCheck(modelTypeName);
        //    }
        //}
        //private void ListModelTypesWithGraphs(Dictionary<string, List<Graph>> results)
        //{
        //    if (!String.IsNullOrWhiteSpace(type))
        //    {
        //        if (!results.ContainsKey(type))
        //            results.Add(type, new List<Graph>());
        //        results[type].Add(this);
        //    }

        //    foreach (var childGraph in childGraphs.Values.Where(x => !String.IsNullOrWhiteSpace(x.type)))
        //    {
        //        childGraph.ListModelTypesWithGraphs(results);
        //    }
        //}

        //private static readonly byte maxSelectRecursive = 2;
        //private static readonly MethodInfo selectMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "Select" && m.GetParameters().Length == 2).First();
        //private static readonly MethodInfo listMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToList" && m.GetParameters().Length == 1).First();
        //private static readonly MethodInfo arrayMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToArray" && m.GetParameters().Length == 1).First();
        //private static readonly MethodInfo hashSetMethod = typeof(Enumerable).GetMethods().Where(m => m.Name == "ToHashSet" && m.GetParameters().Length == 1).First();
        //private static readonly ConcurrentFactoryDictionary<TypeKey, Expression> selectExpressions = new();
        //public Expression<Func<TSource, TTarget>> GenerateSelect<TSource, TTarget>()
        //{
        //    var key = new TypeKey(Signature, typeof(TSource), typeof(TTarget));

        //    var expression = (Expression<Func<TSource, TTarget>>)selectExpressions.GetOrAdd(key, (_) =>
        //    {
        //        return GenerateSelectorExpression<TSource, TTarget>(this);
        //    });

        //    return expression;
        //}
        //public static Expression<Func<TSource, TTarget>> GenerateSelect<TSource, TTarget>(Graph? graph)
        //{
        //    var key = new TypeKey(graph?.Signature, typeof(TSource), typeof(TTarget));

        //    var expression = (Expression<Func<TSource, TTarget>>)selectExpressions.GetOrAdd(key, (_) =>
        //    {
        //        return GenerateSelectorExpression<TSource, TTarget>(graph);
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
        //            if (targetProperty.Type == sourceProperty.Type)
        //            {
        //                //Basic Property
        //                if (graph == null || graph.HasLocalProperty(targetProperty.Name))
        //                {
        //                    Expression sourcePropertyExpression;
        //                    if (sourceProperty.MemberInfo is PropertyInfo propertyInfo)
        //                        sourcePropertyExpression = Expression.Property(sourceExpression, propertyInfo);
        //                    else if (sourceProperty.MemberInfo is FieldInfo fieldInfo)
        //                        sourcePropertyExpression = Expression.Field(sourceExpression, fieldInfo);
        //                    else
        //                        throw new InvalidOperationException();

        //                    MemberBinding binding = Expression.Bind(targetProperty.Type, sourcePropertyExpression);
        //                    bindings.Add(binding);
        //                }
        //            }
        //            else if (sourceProperty.TypeDetail.IsIEnumerableGeneric)
        //            {
        //                //Related Enumerable
        //                var childGraph = graph?.GetChildGraph(targetProperty.Name);
        //                if (childGraph != null)
        //                {
        //                    if (targetProperty.TypeDetail.IsIEnumerableGeneric)
        //                    {
        //                        var sourcePropertyGenericType = sourceProperty.TypeDetail.InnerTypeDetail;
        //                        if (stack.Where(x => x == sourcePropertyGenericType.Type).Count() < maxSelectRecursive)
        //                        {
        //                            var targetPropertyGenericType = targetProperty.TypeDetail.InnerTypeDetail;
        //                            Expression sourcePropertyExpression;
        //                            if (sourceProperty.MemberInfo is PropertyInfo propertyInfo)
        //                                sourcePropertyExpression = Expression.Property(sourceExpression, propertyInfo);
        //                            else if (sourceProperty.MemberInfo is FieldInfo fieldInfo)
        //                                sourcePropertyExpression = Expression.Field(sourceExpression, fieldInfo);
        //                            else
        //                                throw new InvalidOperationException();

        //                            var sourcePropertyParameterExpression = Expression.Parameter(sourcePropertyGenericType.Type, "y");
        //                            var mapperExpression = GenerateSelectorExpression(sourcePropertyGenericType, targetPropertyGenericType, sourcePropertyParameterExpression, childGraph, stack);
        //                            Expression sourcePropertyLambda = Expression.Lambda(mapperExpression, new ParameterExpression[] { sourcePropertyParameterExpression });

        //                            var selectMethodGeneric = selectMethod.MakeGenericMethod(sourcePropertyGenericType.Type, targetPropertyGenericType.Type);
        //                            var callSelect = Expression.Call(selectMethodGeneric, sourcePropertyExpression, sourcePropertyLambda);

        //                            Expression toExpression;
        //                            if (targetProperty.Type.IsArray)
        //                            {
        //                                toExpression = Expression.Call(arrayMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                            }
        //                            else if (targetProperty.TypeDetail.IsIListGeneric || targetProperty.Type.Name == "List`1")
        //                            {
        //                                toExpression = Expression.Call(arrayMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                            }
        //                            else if (targetProperty.TypeDetail.IsISetGeneric || targetProperty.Type.Name == "HashSet`1")
        //                            {
        //                                toExpression = Expression.Call(hashSetMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                            }
        //                            else if (targetProperty.TypeDetail.IsICollectionGeneric)
        //                            {
        //                                toExpression = Expression.Call(arrayMethod.MakeGenericMethod(targetPropertyGenericType.Type), callSelect);
        //                            }
        //                            else
        //                            {
        //                                throw new NotSupportedException($"Graph {nameof(GenerateSelect)} does not support type {targetProperty.Type.GetNiceName()}");
        //                            }

        //                            MemberBinding binding = Expression.Bind(targetProperty.Type, toExpression);
        //                            bindings.Add(binding);
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                //Related Single
        //                var childGraph = graph?.GetChildGraph(targetProperty.Name);
        //                if (childGraph != null)
        //                {
        //                    if (stack.Where(x => x == sourceProperty.Type).Count() < maxSelectRecursive)
        //                    {
        //                        Expression sourcePropertyExpression;
        //                        if (sourceProperty.MemberInfo is PropertyInfo propertyInfo)
        //                            sourcePropertyExpression = Expression.Property(sourceExpression, propertyInfo);
        //                        else if (sourceProperty.MemberInfo is FieldInfo fieldInfo)
        //                            sourcePropertyExpression = Expression.Field(sourceExpression, fieldInfo);
        //                        else
        //                            throw new InvalidOperationException();

        //                        Expression sourceNullIf = Expression.Equal(sourcePropertyExpression, Expression.Constant(null));
        //                        var mapperExpression = GenerateSelectorExpression(sourceProperty.TypeDetail, targetProperty.TypeDetail, sourcePropertyExpression, childGraph, stack);
        //                        Expression sourcePropertyConditionalExpression = Expression.Condition(sourceNullIf, Expression.Convert(Expression.Constant(null), targetProperty.PropertyType), mapperExpression, targetProperty.PropertyType);
        //                        MemberBinding binding = Expression.Bind(targetProperty.MemberInfo, sourcePropertyConditionalExpression);
        //                        bindings.Add(binding);
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    _ = stack.Pop();

        //    var initializer = Expression.MemberInit(Expression.New(typeTarget.Type), bindings);
        //    return initializer;
        //}
    }
}