// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Collections;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract partial class RootTransactStoreProvider<TModel>
    {
        /// <summary>The store this provider writes to, compared by reference with a related model's to tell whether a condition through the relation can run in one statement; <see langword="null"/> when unknown.</summary>
        protected abstract object? Engine { get; }
        object? IProviderRelation.Engine => Engine;

        private readonly ConcurrentFactoryDictionary<string, bool> crossEngineMembers = new();
        private bool IsCrossEngine(ModelMemberDetail member)
        {
            return crossEngineMembers.GetOrAdd(member.Name, (this, member), static (_, state) =>
            {
                var (provider, member) = state;
                var engine = provider.Engine;
                if (engine is null)
                    return false;
                var relatedProvider = provider.GetRelatedProvider(member.Type);
                return relatedProvider is IProviderRelation relation && relation.Engine is not null && !ReferenceEquals(relation.Engine, engine);
            });
        }

        //a calculation key or value, a projected member, or an order through a relation on another engine can't join, so the models load with the members those read, the relations through their own providers, and the query runs in memory
        private Graph? CrossEngineGraph(Query query, out bool orderCrosses)
        {
            orderCrosses = false;
            var finder = new GraphFinder(this, query.Graph is null ? new Graph() : new Graph(query.Graph));
            finder.Read(query.GroupBy);
            finder.Read(query.Value);
            finder.Read(query.Select);
            var crosses = finder.Crosses;
            if (query.Order is not null && query.GroupBy is null && query.Operation is QueryOperation.Many or QueryOperation.First or QueryOperation.Single or QueryOperation.Select)
            {
                finder.Crosses = false;
                foreach (var orderExpression in query.Order.OrderExpressions)
                    finder.Read(orderExpression.Expression);
                orderCrosses = finder.Crosses;
                crosses |= orderCrosses;
            }
            return crosses ? finder.Graph : null;
        }

        private object? QueryCrossEngine(Query query, Graph graph, bool orderCrosses)
        {
            //a projection's order and paging apply to the models, so they stay in the store unless the order is what crosses
            var inStore = query.Operation == QueryOperation.Select && !orderCrosses;
            var models = (IReadOnlyCollection<TModel>)Query(new Query(QueryOperation.Many, modelType, query.Where, inStore ? query.Order : null, inStore ? query.Skip : null, inStore ? query.Take : null, graph))!;
            switch (query.Operation)
            {
                case QueryOperation.Many:
                    return models.Query(null, query.Order, query.Skip, query.Take).ToArray();
                case QueryOperation.First:
                    return models.Query(null, query.Order, query.Skip, query.Take).FirstOrDefault();
                case QueryOperation.Single:
                    return models.Query(null, query.Order, query.Skip, query.Take).SingleOrDefault();
                case QueryOperation.Select:
                    return inStore ? models.Select(query.Select!) : models.Query(null, query.Order, query.Skip, query.Take).Select(query.Select!);
                case QueryOperation.Group:
                    return models.Group(query.GroupBy!, query.Select!, query.Having, query.Order, query.Skip, query.Take);
                default:
                    return models.Calculate(query.Operation, query.GroupBy, query.Value);
            }
        }
        private async Task<object?> QueryCrossEngineAsync(Query query, Graph graph, bool orderCrosses)
        {
            var inStore = query.Operation == QueryOperation.Select && !orderCrosses;
            var models = (IReadOnlyCollection<TModel>)(await QueryAsync(new Query(QueryOperation.Many, modelType, query.Where, inStore ? query.Order : null, inStore ? query.Skip : null, inStore ? query.Take : null, graph)))!;
            switch (query.Operation)
            {
                case QueryOperation.Many:
                    return models.Query(null, query.Order, query.Skip, query.Take).ToArray();
                case QueryOperation.First:
                    return models.Query(null, query.Order, query.Skip, query.Take).FirstOrDefault();
                case QueryOperation.Single:
                    return models.Query(null, query.Order, query.Skip, query.Take).SingleOrDefault();
                case QueryOperation.Select:
                    return inStore ? models.Select(query.Select!) : models.Query(null, query.Order, query.Skip, query.Take).Select(query.Select!);
                case QueryOperation.Group:
                    return models.Group(query.GroupBy!, query.Select!, query.Having, query.Order, query.Skip, query.Take);
                default:
                    return models.Calculate(query.Operation, query.GroupBy, query.Value);
            }
        }

        //the members the expressions read, as a graph: a relation becomes a child graph with what is read of it, and one whose model is on another engine marks the graph as crossing
        private sealed class GraphFinder(RootTransactStoreProvider<TModel> provider, Graph graph) : ExpressionVisitor
        {
            public readonly Graph Graph = graph;
            public bool Crosses;
            private readonly Dictionary<ParameterExpression, (Graph Graph, ModelDetail Model)> parameters = new();
            private ParameterExpression? grouping;

            public void Read(LambdaExpression? lambda)
            {
                if (lambda is null)
                    return;
                if (lambda.Parameters[0].Type == modelType)
                    parameters[lambda.Parameters[0]] = (Graph, modelTypeDetail);
                else
                    grouping = lambda.Parameters[0];
                _ = Visit(lambda.Body);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                //the group's Key is read from the key expression already
                if (node.Expression == grouping)
                    return node;
                var owner = Resolve(node.Expression);
                if (owner is null)
                    return base.VisitMember(node);
                if (owner.Value.Model.TryGetProperty(node.Member.Name, out var member) && member.IsRelated)
                    _ = Relate(owner.Value.Graph, member);
                else
                    owner.Value.Graph.AddMember(node.Member.Name);
                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                //a lambda over a related collection, or over the group's models, reads from that model
                if (node.Method.DeclaringType == typeof(Enumerable) && node.Arguments.Count > 1)
                {
                    var element = node.Arguments[0] == grouping ? (Graph, modelTypeDetail) : Resolve(node.Arguments[0]);
                    if (element is not null)
                    {
                        for (var i = 1; i < node.Arguments.Count; i++)
                        {
                            var argument = node.Arguments[i];
                            if (argument.NodeType == ExpressionType.Quote)
                                argument = ((UnaryExpression)argument).Operand;
                            if (argument is LambdaExpression lambda)
                                parameters[lambda.Parameters[0]] = element.Value;
                        }
                    }
                }
                return base.VisitMethodCall(node);
            }

            private (Graph Graph, ModelDetail Model)? Resolve(Expression? expression)
            {
                switch (expression)
                {
                    case ParameterExpression parameter:
                        return parameters.TryGetValue(parameter, out var found) ? found : null;
                    case MemberExpression member:
                        {
                            var owner = Resolve(member.Expression);
                            if (owner is null || !owner.Value.Model.TryGetProperty(member.Member.Name, out var detail) || !detail.IsRelated)
                                return null;
                            return Relate(owner.Value.Graph, detail);
                        }
                    case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion:
                        return Resolve(conversion.Operand);
                    default:
                        return null;
                }
            }

            private (Graph Graph, ModelDetail Model) Relate(Graph graph, ModelMemberDetail member)
            {
                var relatedType = member.IsEnumerable ? TypeAnalyzer.GetTypeDetail(member.Type).InnerType! : member.Type;
                if (!Crosses && provider.Engine is not null && provider.GetRelatedProvider(relatedType) is IProviderRelation relation && relation.Engine is not null && !ReferenceEquals(relation.Engine, provider.Engine))
                    Crosses = true;
                var child = graph.GetChildGraph(member.Name);
                if (child is null)
                {
                    child = new Graph();
                    graph.AddChildGraph(member.Name, child);
                }
                return (child, ModelAnalyzer.GetModel(relatedType));
            }
        }

        //a condition through a relation on another engine can't join, so its part of the condition runs there first and the identities it matched replace it
        private LambdaExpression? SplitCrossEngineWhere(LambdaExpression? where)
        {
            if (where is null || ModelTypeDetail.RelatedMembers.Count == 0)
                return where;
            var lookups = FindCrossEngineLookups(where);
            if (lookups is null)
                return where;
            foreach (var lookup in lookups)
                lookup.Matches = (IEnumerable)Repo.Query(lookup.Query)!;
            return ReplaceCrossEngineLookups(where, lookups);
        }
        private async Task<LambdaExpression?> SplitCrossEngineWhereAsync(LambdaExpression? where)
        {
            if (where is null || ModelTypeDetail.RelatedMembers.Count == 0)
                return where;
            var lookups = FindCrossEngineLookups(where);
            if (lookups is null)
                return where;
            foreach (var lookup in lookups)
                lookup.Matches = (IEnumerable)(await Repo.QueryAsync(lookup.Query))!;
            return ReplaceCrossEngineLookups(where, lookups);
        }

        private List<CrossEngineLookup>? FindCrossEngineLookups(LambdaExpression where)
        {
            var finder = new LookupFinder(this, where.Parameters[0]);
            _ = finder.Visit(where.Body);
            return finder.Lookups;
        }
        private LambdaExpression ReplaceCrossEngineLookups(LambdaExpression where, List<CrossEngineLookup> lookups)
        {
            var parameter = where.Parameters[0];
            var replacements = new Dictionary<Expression, Expression>();
            foreach (var lookup in lookups)
                replacements.Add(lookup.Node, lookup.BuildReplacement(parameter));
            var body = new Replacer(replacements).Visit(where.Body)!;
            return Expression.Lambda(modelTypeDetail.LambdaDelegateType, body, parameter);
        }

        private sealed class Replacer(Dictionary<Expression, Expression> replacements) : ExpressionVisitor
        {
            public override Expression? Visit(Expression? node)
            {
                if (node is not null && replacements.TryGetValue(node, out var replacement))
                    return replacement;
                return base.Visit(node);
            }
        }

        private enum LookupKind : byte
        {
            Single,
            Any,
            All,
            Aggregate
        }

        private sealed class CrossEngineLookup
        {
            public required Expression Node;
            public required LookupKind Kind;
            public required Query Query;
            public required Type RelatedType;
            public required System.Reflection.MemberInfo ThisMember;
            public string? RelatedForeignIdentity;
            public LambdaExpression? Selector;
            public ExpressionType Comparison;
            public object? ComparedTo;
            public QueryOperation Operation;
            public IEnumerable? Matches;

            public Expression BuildReplacement(ParameterExpression parameter)
            {
                var member = Expression.Convert(Expression.MakeMemberAccess(parameter, ThisMember), objectType);
                switch (Kind)
                {
                    case LookupKind.Single:
                        {
                            var identities = new HashSet<object>();
                            foreach (var match in Matches!)
                                identities.Add(ModelAnalyzer.GetIdentity(RelatedType, match));
                            return Expression.Call(Expression.Constant(identities, hashSetObjectType), hashSetContainsMethod, member);
                        }
                    case LookupKind.Any:
                    case LookupKind.All:
                        {
                            var parents = new HashSet<object>();
                            foreach (var match in Matches!)
                            {
                                var parent = ModelAnalyzer.GetForeignIdentity(RelatedType, RelatedForeignIdentity!, match);
                                if (parent is not null)
                                    parents.Add(parent);
                            }
                            var contains = Expression.Call(Expression.Constant(parents, hashSetObjectType), hashSetContainsMethod, member);
                            //all means no related model fails the condition, which is what was looked up
                            return Kind == LookupKind.All ? Expression.Not(contains) : contains;
                        }
                    default:
                        {
                            var groups = new Dictionary<object, List<object?>>();
                            var getValue = Selector is null ? null : CalculationCommon.CompileBoxed<object>(Expression.Invoke(Selector, Expression.Convert(objectParameter, RelatedType)), objectParameter, false);
                            foreach (var match in Matches!)
                            {
                                var parent = ModelAnalyzer.GetForeignIdentity(RelatedType, RelatedForeignIdentity!, match);
                                if (parent is null)
                                    continue;
                                if (!groups.TryGetValue(parent, out var values))
                                {
                                    values = new List<object?>();
                                    groups.Add(parent, values);
                                }
                                values.Add(getValue is null ? null : getValue(match));
                            }
                            var valueType = Selector?.ReturnType ?? typeof(long);
                            var passing = new HashSet<object>();
                            var failing = new HashSet<object>();
                            foreach (var group in groups)
                            {
                                if (Passes(CalculationCommon.Aggregate(Operation, group.Value, valueType)))
                                    passing.Add(group.Key);
                                else
                                    failing.Add(group.Key);
                            }
                            //a model with nothing related counts or sums to zero, which the condition may accept, so then only the ones that fail are excluded
                            if ((Operation == QueryOperation.Count || Operation == QueryOperation.Sum) && Passes(CalculationCommon.Aggregate(Operation, new List<object?>(), valueType)))
                                return Expression.Not(Expression.Call(Expression.Constant(failing, hashSetObjectType), hashSetContainsMethod, member));
                            return Expression.Call(Expression.Constant(passing, hashSetObjectType), hashSetContainsMethod, member);
                        }
                }
            }

            private bool Passes(object? aggregate)
            {
                if (aggregate is null)
                    return false;
                var comparedTo = ComparedTo is null ? null : TypeAnalyzer.Convert(ComparedTo, aggregate.GetType());
                if (comparedTo is null)
                    return Comparison == ExpressionType.NotEqual;
                var comparison = Comparer<object>.Default.Compare(aggregate, comparedTo);
                return Comparison switch
                {
                    ExpressionType.Equal => comparison == 0,
                    ExpressionType.NotEqual => comparison != 0,
                    ExpressionType.GreaterThan => comparison > 0,
                    ExpressionType.GreaterThanOrEqual => comparison >= 0,
                    ExpressionType.LessThan => comparison < 0,
                    ExpressionType.LessThanOrEqual => comparison <= 0,
                    _ => throw new NotSupportedException($"{Comparison} is not a comparison"),
                };
            }
        }

        private static readonly ParameterExpression objectParameter = Expression.Parameter(typeof(object), "related");

        //walks the and/or structure of the condition; each other node is a leaf that either stays, becomes a lookup, or can't be split
        private sealed class LookupFinder(RootTransactStoreProvider<TModel> provider, ParameterExpression parameter) : ExpressionVisitor
        {
            public List<CrossEngineLookup>? Lookups;

            public override Expression? Visit(Expression? node)
            {
                switch (node)
                {
                    case null:
                        return null;
                    case BinaryExpression { NodeType: ExpressionType.AndAlso or ExpressionType.OrElse or ExpressionType.And or ExpressionType.Or } binary:
                        _ = Visit(binary.Left);
                        _ = Visit(binary.Right);
                        return node;
                    case UnaryExpression { NodeType: ExpressionType.Not or ExpressionType.Quote or ExpressionType.Convert } unary:
                        _ = Visit(unary.Operand);
                        return node;
                    case LambdaExpression lambda:
                        _ = Visit(lambda.Body);
                        return node;
                    default:
                        Leaf(node);
                        return node;
                }
            }

            private void Leaf(Expression node)
            {
                var usage = new UsageFinder(parameter);
                _ = usage.Visit(node);
                if (usage.Relations is null)
                    return;

                ModelMemberDetail? crossMember = null;
                foreach (var name in usage.Relations)
                {
                    var member = modelTypeDetail.GetMember(name);
                    if (!provider.IsCrossEngine(member))
                        continue;
                    if (crossMember is not null || usage.UsesOthers)
                        throw new NotSupportedException($"A condition on {modelTypeDetail.Name} can't mix {member.Name}, which is stored on another engine, with other members in {node}");
                    crossMember = member;
                }
                if (crossMember is null)
                    return;

                var relatedType = crossMember.IsEnumerable ? TypeAnalyzer.GetTypeDetail(crossMember.Type).InnerType! : crossMember.Type;
                var relatedDetail = ModelAnalyzer.GetModel(relatedType);
                if (crossMember.ForeignIdentity is null)
                    throw new Exception($"{crossMember.Type.Name} missing Foreign Identity");

                CrossEngineLookup lookup;
                if (!crossMember.IsEnumerable)
                {
                    //the part of the condition on the related model runs there as a condition on it
                    if (relatedDetail.IdentityMembers.Count != 1)
                        throw new NotSupportedException($"Relational queries support only one identity on {relatedDetail.Type.Name}");
                    var related = Expression.Parameter(relatedType, "related");
                    var body = new RelationReplacer(parameter, crossMember.Name, related).Visit(node);
                    var relatedWhere = Expression.Lambda(relatedDetail.LambdaDelegateType, body, related);
                    lookup = new CrossEngineLookup
                    {
                        Node = node,
                        Kind = LookupKind.Single,
                        Query = new Query(QueryOperation.Many, relatedType, relatedWhere, null, null, null, new Graph(relatedDetail.IdentityMembers[0].Name)),
                        RelatedType = relatedType,
                        ThisMember = modelTypeDetail.GetMember(crossMember.ForeignIdentity).MemberDetail.MemberInfo,
                    };
                }
                else
                {
                    if (modelTypeDetail.IdentityMembers.Count != 1)
                        throw new NotSupportedException($"Relational queries support only one identity on {modelTypeDetail.Type.Name}");
                    var thisIdentity = modelTypeDetail.IdentityMembers[0].MemberDetail.MemberInfo;
                    var foreignMember = relatedDetail.GetMember(crossMember.ForeignIdentity);

                    if (node is MethodCallExpression { Method.Name: "Any" or "All" } call && call.Method.DeclaringType == typeof(Enumerable) && call.Arguments[0] is MemberExpression collection && collection.Expression == parameter)
                    {
                        var predicate = call.Arguments.Count == 2 ? Unquote(call.Arguments[1]) : null;
                        if (call.Method.Name == "All")
                        {
                            if (predicate is null)
                                throw new NotSupportedException($"Cannot convert call expression {call.Method.Name}");
                            predicate = Expression.Lambda(relatedDetail.LambdaDelegateType, Expression.Not(predicate.Body), predicate.Parameters);
                        }
                        lookup = new CrossEngineLookup
                        {
                            Node = node,
                            Kind = call.Method.Name == "All" ? LookupKind.All : LookupKind.Any,
                            Query = new Query(QueryOperation.Many, relatedType, predicate, null, null, null, new Graph(foreignMember.Name)),
                            RelatedType = relatedType,
                            ThisMember = thisIdentity,
                            RelatedForeignIdentity = foreignMember.Name,
                        };
                    }
                    else if (node is BinaryExpression comparison && IsComparison(comparison.NodeType) && (TryGetAggregate(comparison.Left, parameter, out var aggregate, out var predicateOrSelector) || TryGetAggregate(comparison.Right, parameter, out aggregate, out predicateOrSelector)))
                    {
                        var aggregateOnLeft = ReferenceEquals(aggregate, comparison.Left);
                        var other = aggregateOnLeft ? comparison.Right : comparison.Left;
                        var operation = aggregate.Method.Name switch
                        {
                            "Count" or "LongCount" => QueryOperation.Count,
                            "Sum" => QueryOperation.Sum,
                            "Average" => QueryOperation.Average,
                            "Min" => QueryOperation.Min,
                            "Max" => QueryOperation.Max,
                            _ => throw new NotSupportedException($"Cannot convert call expression {aggregate.Method.Name}"),
                        };
                        var isCount = operation == QueryOperation.Count;
                        //the related models are read with the members the value needs, and compared here
                        var graph = new Graph(foreignMember.Name);
                        if (!isCount && predicateOrSelector is not null)
                        {
                            var members = new MemberNameFinder(predicateOrSelector.Parameters[0]);
                            _ = members.Visit(predicateOrSelector.Body);
                            if (members.Nested)
                                graph = null;
                            else if (members.Names is not null)
                                graph.AddMembers(members.Names.ToArray());
                        }
                        lookup = new CrossEngineLookup
                        {
                            Node = node,
                            Kind = LookupKind.Aggregate,
                            Query = new Query(QueryOperation.Many, relatedType, isCount ? predicateOrSelector : null, null, null, null, graph),
                            RelatedType = relatedType,
                            ThisMember = thisIdentity,
                            RelatedForeignIdentity = foreignMember.Name,
                            Selector = isCount ? null : predicateOrSelector,
                            Operation = operation,
                            Comparison = aggregateOnLeft ? comparison.NodeType : Mirror(comparison.NodeType),
                            ComparedTo = Expression.Lambda<Func<object?>>(Expression.Convert(other, objectType)).Compile()(),
                        };
                    }
                    else
                    {
                        throw new NotSupportedException($"A condition on {modelTypeDetail.Name}.{crossMember.Name}, which is stored on another engine, can be Any, All, or Count, Sum, Average, Min, or Max compared to a value, not {node}");
                    }
                }
                Lookups ??= new List<CrossEngineLookup>();
                Lookups.Add(lookup);
            }

            private static LambdaExpression Unquote(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Quote)
                    expression = ((UnaryExpression)expression).Operand;
                return (LambdaExpression)expression;
            }
            private static bool IsComparison(ExpressionType type)
                => type is ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual;
            private static ExpressionType Mirror(ExpressionType type) => type switch
            {
                ExpressionType.GreaterThan => ExpressionType.LessThan,
                ExpressionType.GreaterThanOrEqual => ExpressionType.LessThanOrEqual,
                ExpressionType.LessThan => ExpressionType.GreaterThan,
                ExpressionType.LessThanOrEqual => ExpressionType.GreaterThanOrEqual,
                _ => type,
            };
            private static bool TryGetAggregate(Expression expression, ParameterExpression parameter, out MethodCallExpression aggregate, out LambdaExpression? predicateOrSelector)
            {
                while (expression is UnaryExpression { NodeType: ExpressionType.Convert } conversion)
                    expression = conversion.Operand;
                if (expression is MethodCallExpression { Method.Name: "Count" or "LongCount" or "Sum" or "Average" or "Min" or "Max" } call && call.Method.DeclaringType == typeof(Enumerable) && call.Arguments[0] is MemberExpression collection && collection.Expression == parameter)
                {
                    aggregate = call;
                    predicateOrSelector = call.Arguments.Count == 2 ? Unquote(call.Arguments[1]) : null;
                    return true;
                }
                aggregate = null!;
                predicateOrSelector = null;
                return false;
            }
        }

        //the related model stands in for the relation member in the part of the condition that runs on its engine
        private sealed class RelationReplacer(ParameterExpression parameter, string relationName, ParameterExpression related) : ExpressionVisitor
        {
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == parameter && node.Member.Name == relationName)
                    return related;
                return base.VisitMember(node);
            }
        }

        //what a leaf of the condition reads from the model: which relations, and whether anything else
        private sealed class UsageFinder(ParameterExpression parameter) : ExpressionVisitor
        {
            public HashSet<string>? Relations;
            public bool UsesOthers;

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == parameter)
                {
                    if (modelTypeDetail.TryGetProperty(node.Member.Name, out var member) && member.IsRelated)
                    {
                        Relations ??= new HashSet<string>();
                        _ = Relations.Add(member.Name);
                    }
                    else
                    {
                        UsesOthers = true;
                    }
                    return node;
                }
                return base.VisitMember(node);
            }
            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (node == parameter)
                    UsesOthers = true;
                return node;
            }
        }

        //the members a value reads from its parameter, to load only those, or everything when it reaches into a relation
        private sealed class MemberNameFinder(ParameterExpression parameter) : ExpressionVisitor
        {
            public List<string>? Names;
            public bool Nested;

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression == parameter)
                {
                    Names ??= new List<string>();
                    Names.Add(node.Member.Name);
                    return node;
                }
                if (node.Expression is MemberExpression inner && inner.Expression == parameter)
                    Nested = true;
                return base.VisitMember(node);
            }
        }
    }
}
