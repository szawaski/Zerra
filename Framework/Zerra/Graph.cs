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
        protected string name;
        protected string type;

        protected bool includeAllProperties;
        protected readonly List<string> properties;
        protected readonly List<string> removedProperties;
        protected readonly Dictionary<string, Graph> childGraphs;

        public bool IncludeAllProperties { get { return includeAllProperties; } }
        public IReadOnlyList<string> LocalProperties { get { return properties; } }

        [NonSerialized]
        protected string signature = null;
        public string Signature
        {
            get
            {
                if (this.signature == null)
                {
                    GenerateSignature();
                }
                return this.signature;
            }
        }

        public Graph(Graph graph)
        {
            if (graph != null)
            {
                this.name = graph.name;
                this.includeAllProperties = graph.includeAllProperties;
                this.properties = graph.properties;
                this.removedProperties = graph.removedProperties;
                this.childGraphs = new Dictionary<string, Graph>(graph.childGraphs);
                this.type = graph.type;
                this.signature = graph.signature;
            }
            else
            {
                this.includeAllProperties = true;
                this.properties = new List<string>();
                this.removedProperties = new List<string>();
                this.childGraphs = new Dictionary<string, Graph>();
                this.type = null;
                this.signature = null;
            }
        }

        public Graph() : this(null, false, null, (ICollection<Graph>)null)
        {
            this.signature = "";
        }
        public Graph(bool includeAllProperties) : this(null, includeAllProperties, null, (ICollection<Graph>)null) { }
        public Graph(params string[] properties) : this(null, false, properties, null) { }
        public Graph(bool includeAllProperties, params string[] properties) : this(null, includeAllProperties, properties, null) { }
        public Graph(string name, bool includeAllProperties, params string[] properties) : this(name, includeAllProperties, properties, null) { }

        public Graph(ICollection<string> properties) : this(null, false, properties, null) { }
        public Graph(bool includeAllProperties, ICollection<string> properties) : this(null, includeAllProperties, properties, null) { }
        public Graph(bool includeAllProperties, ICollection<string> properties, ICollection<Graph> childGraphs) : this(null, includeAllProperties, properties, childGraphs) { }
        public Graph(string name, ICollection<string> properties) : this(name, false, properties, null) { }
        public Graph(string name, bool includeAllProperties, ICollection<string> properties) : this(name, includeAllProperties, properties, null) { }
        public Graph(string name, bool includeAllProperties, ICollection<string> properties, ICollection<Graph> childGraphs)
        {
            this.name = name;
            this.includeAllProperties = includeAllProperties;
            this.properties = new List<string>();
            removedProperties = new List<string>();
            this.childGraphs = new Dictionary<string, Graph>();

            AddProperties(properties);
            AddChildGraphs(childGraphs);
        }

        public bool IsEmpty { get { return !includeAllProperties && properties.Count == 0 && childGraphs.Count == 0; } }

        public override bool Equals(object obj)
        {
            if (!(obj is Graph objCasted))
                return false;
            return this.Signature == objCasted.Signature;
        }
        public override int GetHashCode()
        {
            return this.Signature.GetHashCode();
        }

        protected void GenerateSignature()
        {
            var writer = new CharWriteBuffer();
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
        private void GenerateSignatureBuilder(ref CharWriteBuffer writer)
        {
            if (!String.IsNullOrEmpty(this.name))
                writer.Write("N:");
            writer.Write(this.name);

            if (this.includeAllProperties)
                writer.Write("A");

            foreach (var property in this.properties.OrderBy(x => x))
            {
                writer.Write("P:");
                writer.Write(property);
            }

            if (!this.includeAllProperties)
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

        public void AddProperties(params string[] properties) { AddProperties((ICollection<string>)properties); }
        public void AddProperties(ICollection<string> properties)
        {
            if (properties == null || properties.Count == 0)
                return;
            foreach (var property in properties)
                AddProperty(property);
        }

        public void RemoveProperties(params string[] properties) { RemoveProperties((ICollection<string>)properties); }
        public void RemoveProperties(ICollection<string> properties)
        {
            if (properties == null || properties.Count == 0)
                return;
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

        public void AddChildGraphs(params Graph[] graphs) { AddChildGraphs((ICollection<Graph>)graphs); }
        public void AddChildGraphs(ICollection<Graph> graphs)
        {
            if (graphs == null || graphs.Count == 0)
                return;
            foreach (var graph in graphs)
                AddChildGraph(graph);
            this.signature = null;
        }
        public void RemoveChildGraphs(params Graph[] graphs) { RemoveChildGraphs((ICollection<Graph>)graphs); }
        public void RemoveChildGraphs(ICollection<Graph> graphs)
        {
            if (graphs == null || graphs.Count == 0)
                return;
            foreach (var graph in graphs)
                RemoveChildGraph(graph);
            this.signature = null;
        }

        public void ReplaceChildGraphs(params Graph[] graphs)
        {
            if (graphs == null || graphs.Length == 0)
                return;

            var names = graphs.Select(x => x.name);
            if (names.Any(x => String.IsNullOrWhiteSpace(x)))
                throw new InvalidOperationException("Cannot add a graph without a property name.");
            foreach (var graph in graphs)
            {
                if (childGraphs.ContainsKey(graph.name))
                    childGraphs.Remove(graph.name);
                childGraphs.Add(graph.name, graph);
            }
            this.signature = null;
        }
        public void Clear()
        {
            properties.Clear();
            removedProperties.Clear();
            childGraphs.Clear();
            signature = null;
        }

        private void AddProperty(string property)
        {
            if (childGraphs.TryGetValue(property, out Graph childGraph))
            {
                childGraph.includeAllProperties = true;
            }
            else
            {
                if (!this.properties.Contains(property))
                    this.properties.Add(property);
                if (removedProperties.Contains(property))
                    this.properties.Remove(property);
            }
        }
        private void RemoveProperty(string property)
        {
            if (this.properties.Contains(property))
                this.properties.Remove(property);
            if (!removedProperties.Contains(property))
                removedProperties.Add(property);

            if (childGraphs.ContainsKey(property))
                childGraphs.Remove(property);
        }
        private void AddChildGraph(Graph graph)
        {
            if (String.IsNullOrWhiteSpace(graph.name))
                throw new InvalidOperationException("Cannot add a child graph without a name.");

            if (childGraphs.TryGetValue(graph.name, out Graph childGraph))
            {
                childGraph.includeAllProperties |= graph.includeAllProperties;
                childGraph.AddProperties(graph.properties);

                if (this.properties.Contains(childGraph.name))
                {
                    childGraph.includeAllProperties = true;
                    this.properties.Remove(childGraph.name);
                }
            }
            else
            {
                graph.ApplyModelTypeToChildGraphs(this);
                childGraphs.Add(graph.name, graph);
            }
        }
        private void RemoveChildGraph(Graph graph)
        {
            if (this.properties.Contains(graph.name))
                this.properties.Remove(graph.name);
            if (!removedProperties.Contains(graph.name))
                removedProperties.Add(graph.name);

            if (childGraphs.ContainsKey(graph.name))
                childGraphs.Remove(graph.name);
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
                if (!childGraphs.TryGetValue(member.Name, out Graph childGraph))
                {
                    childGraph = new Graph();
                    childGraph.name = member.Name;
                    if (member.MemberType == MemberTypes.Property)
                        childGraph.type = ((PropertyInfo)member).PropertyType.FullName;
                    else
                        childGraph.type = ((FieldInfo)member).FieldType.FullName;
                    childGraphs.Add(member.Name, childGraph);
                }
                if (this.properties.Contains(childGraph.name))
                {
                    childGraph.includeAllProperties = true;
                    this.properties.Remove(childGraph.name);
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
                if (childGraphs.TryGetValue(member.Name, out Graph childGraph))
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
            return Convert(this, type);
        }

        public bool HasLocalProperty(string name)
        {
            return (this.includeAllProperties && !this.removedProperties.Contains(name)) || this.properties.Contains(name);
        }
        public bool HasProperty(string name)
        {
            return (this.includeAllProperties && !this.removedProperties.Contains(name)) || this.properties.Contains(name) || childGraphs.ContainsKey(name);
        }
        public bool HasChildGraph(string name)
        {
            return this.properties.Contains(name) || childGraphs.ContainsKey(name);
        }

        public Graph GetChildGraph(string name)
        {
            if (childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out Graph childGraph))
                return null;
            return childGraph;
        }
        public Graph GetChildGraph(string name, Type type)
        {
            if (childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out Graph nonGenericGraph))
                return null;
            return Convert(nonGenericGraph, type);
        }
        public Graph<T> GetChildGraph<T>(string name)
        {
            if (childGraphs.Count == 0)
                return null;
            if (!childGraphs.TryGetValue(name, out Graph nonGenericGraph))
                return null;
            return new Graph<T>(nonGenericGraph);
        }

        public Graph GetChildGraphNotNull(string name)
        {
            if (childGraphs.Count == 0)
                return new Graph(name, true);
            if (childGraphs.TryGetValue(name, out Graph childGraph))
                return childGraph;
            if (properties.Contains(name))
                return new Graph(name, true);
            return null;
        }
        public Graph GetChildGraphNotNull(string name, Type type)
        {
            if (childGraphs.Count == 0)
                return Convert(new Graph(name, true), type);
            if (childGraphs.TryGetValue(name, out Graph nonGenericGraph))
                return Convert(nonGenericGraph, type);
            if (properties.Contains(name))
                return Convert(new Graph(name, true), type);
            return null;
        }
        public Graph<T> GetChildGraphNotNull<T>(string name)
        {
            if (childGraphs.Count == 0)
                return new Graph<T>(name, true);
            if (childGraphs.TryGetValue(name, out Graph nonGenericGraph))
                return new Graph<T>(nonGenericGraph);
            if (properties.Contains(name))
                return new Graph<T>(name, true);
            return null;
        }

        public static Graph Convert(Graph graph, Type type)
        {
            var constructor = TypeAnalyzer.GetGenericTypeDetail(typeof(Graph<>), type).GetConstructor(new Type[] { typeof(Graph) });
            Graph genericGraph = (Graph)constructor.Creator(new object[] { graph });
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
            var writer = new CharWriteBuffer();
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
        private void ToString(ref CharWriteBuffer writer, int depth)
        {
            foreach (var property in this.properties)
            {
                for (int i = 0; i < depth * 3; i++)
                    writer.Write(' ');

                writer.Write(property);
                writer.Write(Environment.NewLine);
            }
            foreach (var childGraph in this.childGraphs.Values)
            {
                for (int i = 0; i < depth * 3; i++)
                    writer.Write(' ');

                writer.Write(childGraph.name);
                writer.Write(Environment.NewLine);
                childGraph.ToString(ref writer, depth + 1);
            }
        }

        public virtual Type GetModelType()
        {
            if (!String.IsNullOrWhiteSpace(type))
            {
                return Discovery.GetTypeFromName(type);
            }
            return null;
        }

        public void ApplyModelType(Type type)
        {
            this.type = type.FullName;
            foreach (var childGraph in childGraphs.Values)
            {
                childGraph.ApplyModelTypeToChildGraphs(this);
            }
        }
        private void ApplyModelTypeToChildGraphs(Graph parent)
        {
            type = null;
            Type modelType = parent.GetModelType();
            if (modelType != null)
            {
                var modelInfo = TypeAnalyzer.GetType(modelType);
                if (modelInfo.TryGetMember(name, out MemberDetail property))
                {
                    Type subModelType = property.Type;
                    var subModelTypeDetails = TypeAnalyzer.GetType(subModelType);
                    if (subModelTypeDetails.IsIEnumerableGeneric)
                    {
                        subModelType = subModelTypeDetails.IEnumerableGenericInnerType;
                    }
                    type = subModelType.AssemblyQualifiedName;
                    foreach (var childGraph in childGraphs.Values)
                    {
                        childGraph.ApplyModelTypeToChildGraphs(this);
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
                var allProperties = graphs.Value.SelectMany(x => x.properties).ToArray();
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
                if (!results.Keys.Contains(type))
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
        private static readonly ConcurrentFactoryDictionary<TypeKey, Expression> selectExpressions = new ConcurrentFactoryDictionary<TypeKey, Expression>();
        public Expression<Func<TSource, TTarget>> GenerateSelect<TSource, TTarget>()
        {
            var key = new TypeKey(this.Signature, typeof(TSource), typeof(TTarget));

            var expression = (Expression<Func<TSource, TTarget>>)selectExpressions.GetOrAdd(key, (factoryKey) =>
            {
                return GenerateSelectorExpression<TSource, TTarget>(this);
            });

            return expression;
        }
        private static Expression<Func<TSource, TTarget>> GenerateSelectorExpression<TSource, TTarget>(Graph graph)
        {
            Type typeSource = typeof(TSource);
            Type typeTarget = typeof(TTarget);

            ParameterExpression sourceParameterExpression = Expression.Parameter(typeSource, "x");

            Expression selectorExpression = GenerateSelectorExpression(typeSource, typeTarget, sourceParameterExpression, graph, new Stack<Type>());

            Expression<Func<TSource, TTarget>> lambda = Expression.Lambda<Func<TSource, TTarget>>(selectorExpression, sourceParameterExpression);
            return lambda;
        }
        private static Expression GenerateSelectorExpression(Type typeSource, Type typeTarget, Expression sourceExpression, Graph graph, Stack<Type> stack)
        {
            var sourceProperties = typeSource.GetProperties();
            var targetProperites = typeTarget.GetProperties();

            stack.Push(typeSource);

            List<MemberBinding> bindings = new List<MemberBinding>();

            foreach (PropertyInfo targetProperty in targetProperites.Where(x => x.CanWrite))
            {
                PropertyInfo sourceProperty = sourceProperties.FirstOrDefault(x => x.CanRead && x.Name == targetProperty.Name);

                if (sourceProperty != null)
                {
                    if (targetProperty.PropertyType == sourceProperty.PropertyType)
                    {
                        //Basic Property
                        if (graph.HasLocalProperty(targetProperty.Name))
                        {
                            Expression sourcePropertyExpression = Expression.Property(sourceExpression, sourceProperty);

                            MemberBinding binding = Expression.Bind(targetProperty, sourcePropertyExpression);
                            bindings.Add(binding);
                        }
                    }
                    else if (sourceProperty.PropertyType.GetInterface(typeof(IEnumerable<>).MakeGenericType(targetProperty.PropertyType).Name) != null)
                    {
                        //Related Enumerable
                        if (graph.HasChildGraph(targetProperty.Name))
                        {
                            if (targetProperty.PropertyType.IsGenericType)
                            {
                                Type sourcePropertyGenericType = sourceProperty.PropertyType.GetGenericArguments()[0];
                                Graph childGraph = graph.GetChildGraph(targetProperty.Name);

                                if (stack.Where(x => x == sourcePropertyGenericType).Count() < maxSelectRecursive)
                                {
                                    Type targetPropertyGenericType = targetProperty.PropertyType.GetGenericArguments()[0];
                                    Expression sourcePropertyExpression = Expression.Property(sourceExpression, sourceProperty);

                                    ParameterExpression sourcePropertyParameterExpression = Expression.Parameter(sourcePropertyGenericType, "y");
                                    Expression mapperExpression = GenerateSelectorExpression(sourcePropertyGenericType, targetPropertyGenericType, sourcePropertyParameterExpression, childGraph, stack);
                                    Expression sourcePropertyLambda = Expression.Lambda(mapperExpression, new ParameterExpression[] { sourcePropertyParameterExpression });

                                    MethodInfo selectMethodGeneric = selectMethod.MakeGenericMethod(sourcePropertyGenericType, targetPropertyGenericType);
                                    MethodCallExpression callSelect = Expression.Call(selectMethodGeneric, sourcePropertyExpression, sourcePropertyLambda);

                                    MethodInfo listMethodGeneric = listMethod.MakeGenericMethod(targetPropertyGenericType);
                                    MethodCallExpression listSelect = Expression.Call(listMethodGeneric, callSelect);

                                    MemberBinding binding = Expression.Bind(targetProperty, listSelect);
                                    bindings.Add(binding);
                                }
                            }
                        }
                    }
                    else
                    {
                        //Related Single
                        if (graph.HasChildGraph(targetProperty.Name))
                        {
                            Graph childGraph = graph.GetChildGraph(targetProperty.Name);
                            if (stack.Where(x => x == sourceProperty.PropertyType).Count() < maxSelectRecursive)
                            {
                                Expression sourcePropertyExpression = Expression.Property(sourceExpression, sourceProperty);
                                Expression sourceNullIf = Expression.Equal(sourcePropertyExpression, Expression.Constant(null));
                                Expression mapperExpression = GenerateSelectorExpression(sourceProperty.PropertyType, targetProperty.PropertyType, sourcePropertyExpression, childGraph, stack);
                                Expression sourcePropertyConditionalExpression = Expression.Condition(sourceNullIf, Expression.Convert(Expression.Constant(null), targetProperty.PropertyType), mapperExpression, targetProperty.PropertyType);
                                MemberBinding binding = Expression.Bind(targetProperty, sourcePropertyConditionalExpression);
                                bindings.Add(binding);
                            }
                        }
                    }
                }
            }

            stack.Pop();

            MemberInitExpression initializer = Expression.MemberInit(Expression.New(typeTarget), bindings);
            return initializer;
        }
    }

    public class Graph<T> : Graph
    {
        public Graph(Graph graph)
            : base(graph)
        {
        }

        public Graph() : this(null, false, (string[])null) { }
        public Graph(bool includeProperties) : this(null, includeProperties, (string[])null) { }
        public Graph(string name, bool includeProperties) : this(name, includeProperties, (string[])null) { }
        public Graph(params string[] properties) : this(null, false, properties) { }
        public Graph(bool includeAllProperties, params string[] properties) : this(null, includeAllProperties, properties) { }
        public Graph(string name, bool includeAllProperties, params string[] properties)
            : base(name, includeAllProperties, null)
        {
            this.type = GetModelType().FullName;
            AddProperties(properties);
        }

        public Graph(params Expression<Func<T, object>>[] properties) : this(null, false, (ICollection<Expression<Func<T, object>>>)properties) { }
        public Graph(string name, params Expression<Func<T, object>>[] properties) : this(name, false, (ICollection<Expression<Func<T, object>>>)properties) { }
        public Graph(bool includeAllProperties, params Expression<Func<T, object>>[] properties) : this(null, includeAllProperties, (ICollection<Expression<Func<T, object>>>)properties) { }
        public Graph(string name, bool includeAllProperties, params Expression<Func<T, object>>[] properties) : this(name, includeAllProperties, (ICollection<Expression<Func<T, object>>>)properties) { }

        public Graph(ICollection<Expression<Func<T, object>>> properties) : this(null, false, properties) { }
        public Graph(string name, ICollection<Expression<Func<T, object>>> properties) : this(name, false, properties) { }
        public Graph(bool includeAllProperties, ICollection<Expression<Func<T, object>>> properties) : this(null, includeAllProperties, properties) { }
        public Graph(string name, bool includeAllProperties, ICollection<Expression<Func<T, object>>> properties)
            : base(name, includeAllProperties, (ICollection<string>)null, null)
        {
            this.type = GetModelType().FullName;
            AddProperties(properties);
        }

        private static void ReadPropertyExpression(Expression property, Stack<MemberInfo> members)
        {
            if (property.NodeType != ExpressionType.Lambda)
                throw new ArgumentException("Invalid property expression");
            var lambda = property as LambdaExpression;
            ReadPropertyExpressionMember(lambda.Body, members);
        }
        private static void ReadPropertyExpressionMember(Expression property, Stack<MemberInfo> members)
        {
            if (property.NodeType == ExpressionType.Parameter)
            {
                return;
            }
            else if (property.NodeType == ExpressionType.Call)
            {
                var call = property as MethodCallExpression;
                if (call.Arguments.Count != 2 || call.Object != null)
                    throw new ArgumentException("Invalid property expression");
                if (call.Arguments[0].NodeType != ExpressionType.MemberAccess)
                    throw new ArgumentException("Invalid property expression");
                if (call.Arguments[1].NodeType != ExpressionType.Lambda)
                    throw new ArgumentException("Invalid property expression");

                var lambda = call.Arguments[1] as LambdaExpression;
                ReadPropertyExpressionMember(lambda.Body, members);

                var member = call.Arguments[0] as MemberExpression;
                members.Push(member.Member);
                ReadPropertyExpressionMember(member.Expression, members);
            }
            else
            {
                if (property.NodeType == ExpressionType.Convert)
                {
                    var convert = property as UnaryExpression;
                    property = convert.Operand;
                }
                if (property.NodeType != ExpressionType.MemberAccess)
                    throw new ArgumentException("Invalid property expression");

                var member = property as MemberExpression;
                members.Push(member.Member);
                ReadPropertyExpressionMember(member.Expression, members);
            }
        }

        public void AddProperties(params Expression<Func<T, object>>[] properties) { AddProperties((ICollection<Expression<Func<T, object>>>)properties); }
        public void AddProperties(ICollection<Expression<Func<T, object>>> properties)
        {
            if (properties == null || properties.Count == 0)
                return;

            var members = new Stack<MemberInfo>();
            foreach (var property in properties)
            {
                ReadPropertyExpression(property, members);
                AddMembers(members);
                members.Clear();
            }
            this.signature = null;
        }
        public void RemoveProperties(params Expression<Func<T, object>>[] properties) { RemoveProperties((ICollection<Expression<Func<T, object>>>)properties); }
        public void RemoveProperties(ICollection<Expression<Func<T, object>>> properties)
        {
            if (properties == null || properties.Count == 0)
                return;

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