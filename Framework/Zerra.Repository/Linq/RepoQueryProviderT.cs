// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    internal sealed class RepoQueryProvider<TModel> : RepoQueryProvider where TModel : class, new()
    {
        private readonly IRepoInternal repo;
        private readonly Graph? graph;
        private readonly LinqTemporal? temporal;

        public RepoQueryProvider(IRepoInternal repo, Graph? graph, LinqTemporal? temporal)
        {
            this.repo = repo;
            this.graph = graph;
            this.temporal = temporal;
        }

        public override object? Execute<TResult>(Expression expression, LinqTerminal? terminal, LambdaExpression? argument)
        {
            var query = Read<TResult>(expression, terminal, argument);
            return Finish<TResult>(query, repo.Query(query));
        }
        public override async Task<object?> ExecuteAsync<TResult>(Expression expression, LinqTerminal? terminal, LambdaExpression? argument, CancellationToken cancellationToken)
        {
            var query = Read<TResult>(expression, terminal, argument);
            return Finish<TResult>(query, await repo.QueryAsync(query, cancellationToken));
        }
        public override IEnumerable<TElement> ExecuteSequence<TElement>(Expression expression)
        {
            if (IsChunk(expression, out var chunked, out var size))
                return Chunks<TElement>((IEnumerable)repo.Query(Read<object?>(chunked, null, null))!, size);
            var query = Read<TElement>(expression, null, null);
            var elements = (IEnumerable<TElement>)repo.Query(query)!;
            return query.DefaultIfEmpty ? WithDefault(elements, query) : elements;
        }
        public override async Task<IEnumerable<TElement>> ExecuteSequenceAsync<TElement>(Expression expression, CancellationToken cancellationToken)
        {
            if (IsChunk(expression, out var chunked, out var size))
                return Chunks<TElement>((IEnumerable)(await repo.QueryAsync(Read<object?>(chunked, null, null), cancellationToken))!, size);
            var query = Read<TElement>(expression, null, null);
            var elements = (IEnumerable<TElement>)(await repo.QueryAsync(query, cancellationToken))!;
            return query.DefaultIfEmpty ? WithDefault(elements, query) : elements;
        }
        public override IEnumerable<TElement> ExecuteStream<TElement>(Expression expression)
        {
            if (IsChunk(expression, out var chunked, out var size))
                return Chunks<TElement>((IEnumerable)repo.QueryStream(Read<object?>(chunked, null, null)), size);
            var query = Read<TElement>(expression, null, null);
            var elements = (IEnumerable<TElement>)repo.QueryStream(query);
            return query.DefaultIfEmpty ? WithDefault(elements, query) : elements;
        }
        public override IAsyncEnumerable<TElement> ExecuteStreamAsync<TElement>(Expression expression, CancellationToken cancellationToken)
        {
            if (IsChunk(expression, out var chunked, out var size))
                return ChunksAsync<TElement>((IAsyncEnumerable<object?>)repo.QueryStreamAsync(Read<object?>(chunked, null, null), cancellationToken), size, cancellationToken);
            var query = Read<TElement>(expression, null, null);
            var elements = (IAsyncEnumerable<TElement>)repo.QueryStreamAsync(query, cancellationToken);
            return query.DefaultIfEmpty ? WithDefaultAsync(elements, query, cancellationToken) : elements;
        }
        //Chunk is the last call, its arrays made of the elements as they are read
        private static bool IsChunk(Expression expression, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Expression? chunked, out int size)
        {
            if (expression is MethodCallExpression { Method.Name: "Chunk" } chunk && chunk.Method.DeclaringType == typeof(System.Linq.Queryable))
            {
                chunked = chunk.Arguments[0];
                size = LinqQuery.Count(chunk.Arguments[1]);
                if (size < 1)
                    throw new ArgumentOutOfRangeException(nameof(size));
                return true;
            }
            chunked = null;
            size = 0;
            return false;
        }
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "An array of the elements the query already reads.")]
        private static Array NewChunk<TElement>(int length) => Array.CreateInstance(typeof(TElement).GetElementType()!, length);
        private static IEnumerable<TElement> Chunks<TElement>(IEnumerable elements, int size)
        {
            var chunk = new List<object?>(size);
            foreach (var element in elements)
            {
                chunk.Add(element);
                if (chunk.Count == size)
                {
                    yield return ToChunk<TElement>(chunk);
                    chunk.Clear();
                }
            }
            if (chunk.Count > 0)
                yield return ToChunk<TElement>(chunk);
        }
        private static async IAsyncEnumerable<TElement> ChunksAsync<TElement>(IAsyncEnumerable<object?> elements, int size, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var chunk = new List<object?>(size);
            await foreach (var element in elements.WithCancellation(cancellationToken))
            {
                chunk.Add(element);
                if (chunk.Count == size)
                {
                    yield return ToChunk<TElement>(chunk);
                    chunk.Clear();
                }
            }
            if (chunk.Count > 0)
                yield return ToChunk<TElement>(chunk);
        }
        private static TElement ToChunk<TElement>(List<object?> elements)
        {
            var chunk = NewChunk<TElement>(elements.Count);
            for (var i = 0; i < elements.Count; i++)
                chunk.SetValue(elements[i], i);
            return (TElement)(object)chunk;
        }

        //no element is one element of the default
        private static IEnumerable<TElement> WithDefault<TElement>(IEnumerable<TElement> elements, LinqQuery query)
        {
            var any = false;
            foreach (var element in elements)
            {
                any = true;
                yield return element;
            }
            if (!any)
                yield return query.HasDefaultValue ? (TElement)query.DefaultValue! : default!;
        }
        private static async IAsyncEnumerable<TElement> WithDefaultAsync<TElement>(IAsyncEnumerable<TElement> elements, LinqQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var any = false;
            await foreach (var element in elements.WithCancellation(cancellationToken))
            {
                any = true;
                yield return element;
            }
            if (!any)
                yield return query.HasDefaultValue ? (TElement)query.DefaultValue! : default!;
        }

        internal override LinqSubquery ReadSubquery(Expression expression)
        {
            if (temporal is not null)
                throw new NotSupportedException("A query a condition looks in reads the current state of models");
            var query = new LinqQuery<TModel, object?>(expression, null, null, graph);
            if (query.GroupBy is not null || !query.IsScalarSelect)
                throw new NotSupportedException("A query a condition looks in selects one value, such as Select(x => x.ID)");
            query.ReadAsSubquery(null);
            return new LinqSubquery(repo, query);
        }

        internal override LinqSet ReadSet(LinqSetOperation operation, LinqQuery left, Expression expression)
        {
            if (graph is not null || temporal is not null)
                throw new NotSupportedException("A query combined with a set operation reads the current state of models without a graph");
            var query = new LinqQuery<TModel, object?>(expression, null, null);
            query.ReadAsSetPart();
            return new LinqSet(repo, operation, left, query);
        }

        internal override LinqQuery ReadNumbered(Expression expression, string operation)
        {
            if (graph is not null || temporal is not null)
                throw new NotSupportedException($"A query {operation} reads is of the current state of models without a graph");
            var query = new LinqQuery<TModel, object?>(expression, null, null);
            query.ReadAsNumberedPart(operation);
            return query;
        }
        internal override IRepoInternal Repo => repo;

        internal override LinqJoin ReadJoin(LinqJoinKind kind, LinqQuery left, Expression expression, LambdaExpression leftKey, LambdaExpression rightKey, Expression result, ParameterExpression leftElement, ParameterExpression rightElement)
        {
            if (graph is not null || temporal is not null)
                throw new NotSupportedException("A query paired by a join reads the current state of models without a graph");
            var query = new LinqQuery<TModel, object?>(expression, null, null);
            query.ReadAsSetPart();
            return new LinqJoin(repo, kind, left, query, leftKey, rightKey, result, leftElement, rightElement);
        }

        private LinqQuery Read<TResult>(Expression expression, LinqTerminal? terminal, LambdaExpression? argument)
        {
            //the first SelectMany is where the query goes over to the related model
            MethodCallExpression? selectMany = null;
            var node = expression;
            while (node is MethodCallExpression call && call.Arguments.Count > 0)
            {
                //a SelectMany of a GroupJoin's group is a join
                if (call.Method.Name == "SelectMany" && call.Method.DeclaringType == typeof(System.Linq.Queryable) && call.Arguments[0] is not MethodCallExpression { Method.Name: "GroupJoin" })
                    selectMany = call;
                node = call.Arguments[0];
            }
            if (temporal is not null)
            {
                if (selectMany is not null)
                    throw new NotSupportedException("SelectMany reads the current state of models");
                //an event query's graph is of the model of each event, which the query hands its store with the rest of what it reads of the history
                if (temporal.Events)
                    return new LinqQuery<EventModel<TModel>, TResult>(expression, terminal, argument, null, temporal);
                return new LinqQuery<TModel, TResult>(expression, terminal, argument, graph, temporal);
            }
            if (selectMany is null)
                return new LinqQuery<TModel, TResult>(expression, terminal, argument, graph);
            return ReadRelated<TResult>(selectMany, expression, terminal, argument);
        }

        //a query of the related model, whose rows are those of the parents, so all that follows runs in its store as in any query of it
        private LinqQuery ReadRelated<TResult>(MethodCallExpression selectMany, Expression expression, LinqTerminal? terminal, LambdaExpression? argument)
        {
            if (graph is not null)
                throw new NotSupportedException("A graph and a SelectMany both say what a query reads, so a query has one or the other");
            var selector = Quoted(selectMany.Arguments[1]);
            if (selectMany.Arguments.Count > 3 || selector.Parameters.Count != 1)
                throw new NotSupportedException("SelectMany with more than the model isn't supported in a repository query");
            var parentParameter = selector.Parameters[0];

            //what the SelectMany says of the rows, from the outside in
            LambdaExpression? select = null;
            LambdaExpression? where = null;
            var orders = new List<LinqOrder>();
            var source = selector.Body;
            while (source is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
                source = conversion.Operand;
            if (source is MethodCallExpression { Method.Name: "ToArray" or "ToList" } wrap && wrap.Method.DeclaringType == typeof(Enumerable) && wrap.Arguments.Count == 1)
                source = wrap.Arguments[0];
            while (source is MethodCallExpression chain && chain.Method.DeclaringType == typeof(Enumerable) && chain.Arguments.Count == 2)
            {
                if (chain.Method.Name is "Skip" or "Take")
                    throw new NotSupportedException("Skip and Take inside a SelectMany would be for each model, so read that as a Select of the collection, or page after the SelectMany");
                if (chain.Method.Name is not ("Select" or "Where" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending"))
                    break;
                var lambda = Quoted(chain.Arguments[1]);
                if (lambda.Parameters.Count != 1)
                    break;
                switch (chain.Method.Name)
                {
                    case "Select":
                        if (select is not null || where is not null || orders.Count > 0)
                            throw new NotSupportedException("The rows of a SelectMany are filtered and ordered, then selected from once, or filter after it");
                        select = lambda;
                        break;
                    case "Where":
                        where = where is null ? lambda : LinqQuery.AppendAnd(lambda, where);
                        break;
                    default:
                        orders.Insert(0, new LinqOrder(lambda, chain.Method.Name is "OrderByDescending" or "ThenByDescending"));
                        break;
                }
                source = chain.Arguments[0];
            }

            var model = ModelAnalyzer.GetModel(typeof(TModel));
            if (source is not MemberExpression member || member.Expression != parentParameter || !model.TryGetProperty(member.Member.Name, out var relation) || !relation.IsRelated || !relation.IsEnumerable)
                throw new NotSupportedException("SelectMany reads a related collection of the model, such as x => x.Lines");
            if ((where is not null && LinqPlan.Uses(where.Body, parentParameter)) || (select is not null && LinqPlan.Uses(select.Body, parentParameter)))
                throw new NotSupportedException($"The rows of {relation.Name} are filtered and built from themselves alone, or from the model with the result selector");
            if (!repo.TryGetProvider(relation.ActualType, out var related))
                throw new Exception($"No provider for {relation.ActualType.FullName}");
            var relatedModel = ModelAnalyzer.GetModel(relation.ActualType);

            //the parents, as a query of their keys the condition on the rows looks in
            var parents = new LinqQuery<TModel, object?>(selectMany.Arguments[0], (LinqTerminal?)null, null, null);
            if (parents.Select is not null || parents.GroupBy is not null || parents.Distinct)
                throw new NotSupportedException("SelectMany reads the rows of the models, so it comes before a Select, Distinct, or GroupBy");
            var row = Expression.Parameter(relation.ActualType, "x");
            Expression key;
            if (model.IdentityMembers.Count == 1)
            {
                parents.ReadAsSubquery(model.IdentityMembers[0]);
                key = Expression.Convert(Expression.MakeMemberAccess(row, relatedModel.GetMember(relation.ForeignIdentities![0]).MemberInfo), typeof(object));
            }
            else
            {
                //an identity of several members is selected as one key of them, which the rows' foreign keys are looked for as
                parents.ReadAsSubquery(null);
                var parentKey = Expression.Parameter(typeof(TModel), "x");
                parents.Select = Expression.Lambda<Func<TModel, LinqKeyRow>>(LinqKeyRow.New([.. model.IdentityMembers.Select(x => (Expression)Expression.MakeMemberAccess(parentKey, x.MemberInfo))]), parentKey);
                key = LinqKeyRow.New([.. relation.ForeignIdentities!.Select(x => (Expression)Expression.MakeMemberAccess(row, relatedModel.GetMember(x).MemberInfo))]);
            }
            var condition = Expression.Lambda(relatedModel.LambdaDelegateType, Expression.Call(Expression.Constant(new LinqSubquery(repo, parents)), LinqSubquery.ContainsMethod, key), row);

            Expression? selectBody = null;
            ParameterExpression? selectParameter = null;
            if (selectMany.Arguments.Count == 3)
            {
                if (select is not null)
                    throw new NotSupportedException("SelectMany with a result selector builds from the rows, so select in one or the other");
                var result = Quoted(selectMany.Arguments[2]);
                selectParameter = result.Parameters[1];
                selectBody = result.Body;
                if (LinqPlan.Uses(selectBody, result.Parameters[0]))
                {
                    //the model is read through the relation back to it, which is the same row
                    ModelMemberDetail? back = null;
                    foreach (var candidate in relatedModel.RelatedMembers)
                    {
                        if (!candidate.IsEnumerable && candidate.ActualType == typeof(TModel) && candidate.ForeignIdentityNames == relation.ForeignIdentityNames)
                            back = candidate;
                    }
                    if (back is null)
                        throw new NotSupportedException($"A result selector reading {model.Type.Name} needs a relation from {relatedModel.Type.Name} back to it");
                    selectBody = new ParameterReplacer(result.Parameters[0], Expression.MakeMemberAccess(selectParameter, back.MemberInfo)).Visit(selectBody);
                }
            }

            return related.ReadElements<TResult>(expression, new LinqElements
            {
                Root = selectMany,
                Condition = condition,
                Where = where,
                Orders = orders.Count == 0 ? null : orders.ToArray(),
                Select = select,
                SelectBody = selectBody,
                SelectParameter = selectParameter,
            }, terminal, argument);
        }

        private static LambdaExpression Quoted(Expression argument)
            => (LambdaExpression)(argument.NodeType == ExpressionType.Quote ? ((UnaryExpression)argument).Operand : argument);

        private sealed class ParameterReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == parameter ? replacement : node;
        }

        //the value the ending call gives from what the store gave: the elements it picks from, the count or the answer as the call's type, or the aggregate
        private static object? Finish<TResult>(LinqQuery query, object? result)
        {
            var orDefault = query.HasDefaultValue ? (TResult)query.DefaultValue! : default!;
            if (query.DefaultIfEmpty)
            {
                //no element is one element of the default, which a count counts and an aggregate is of
                switch (query.Terminal)
                {
                    case LinqTerminal.Count:
                    case LinqTerminal.LongCount:
                        if (Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture) == 0)
                            result = 1L;
                        break;
                    case LinqTerminal.Any:
                        return true;
                    case LinqTerminal.Sum:
                    case LinqTerminal.Min:
                    case LinqTerminal.Max:
                    case LinqTerminal.Average:
                        if (result is null)
                            return orDefault;
                        break;
                    default:
                        if (!((IEnumerable<TResult>)result!).Any())
                            return orDefault;
                        break;
                }
            }
            switch (query.Terminal)
            {
                case LinqTerminal.First:
                    return ((IEnumerable<TResult>)result!).First();
                case LinqTerminal.FirstOrDefault:
                    return ((IEnumerable<TResult>)result!).FirstOrDefault(orDefault);
                case LinqTerminal.Single:
                    return ((IEnumerable<TResult>)result!).Single();
                case LinqTerminal.SingleOrDefault:
                    return ((IEnumerable<TResult>)result!).SingleOrDefault(orDefault);
                case LinqTerminal.Last:
                    return ((IEnumerable<TResult>)result!).First();
                case LinqTerminal.LastOrDefault:
                    return ((IEnumerable<TResult>)result!).FirstOrDefault(orDefault);
                case LinqTerminal.MinBy:
                case LinqTerminal.MaxBy:
                    {
                        //no element is null where the element can be, as LINQ gives
                        var elements = (IEnumerable<TResult>)result!;
                        if (typeof(TResult).IsValueType && Nullable.GetUnderlyingType(typeof(TResult)) is null)
                            return elements.First();
                        return elements.FirstOrDefault();
                    }
                case LinqTerminal.ElementAt:
                    {
                        var elements = (IEnumerable<TResult>)result!;
                        if (!elements.Any())
                            throw new ArgumentOutOfRangeException("index");
                        return elements.First();
                    }
                case LinqTerminal.ElementAtOrDefault:
                    return ((IEnumerable<TResult>)result!).FirstOrDefault();
                case LinqTerminal.Any:
                    return query.Negated ? !(bool)result! : result;
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    return result!.GetType() == typeof(TResult) ? result : TypeAnalyzer.Convert(result, typeof(TResult));
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    if (result is null)
                    {
                        //no models give no minimum, maximum, or average, which LINQ refuses unless the value can be null
                        if (typeof(TResult).IsValueType && Nullable.GetUnderlyingType(typeof(TResult)) is null)
                            throw new InvalidOperationException("Sequence contains no elements");
                        return null;
                    }
                    return result.GetType() == typeof(TResult) || result.GetType() == Nullable.GetUnderlyingType(typeof(TResult)) ? result : TypeAnalyzer.Convert(result, typeof(TResult));
                default:
                    return result;
            }
        }
    }
}
