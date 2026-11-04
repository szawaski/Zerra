// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Zerra.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// A LINQ query of <typeparamref name="TModel"/> giving elements or a value of <typeparamref name="TResult"/>, which runs on an engine or over loaded models with both types known.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TResult">The type of the elements, or of the value the query ends in.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LinqQuery<TModel, TResult> : LinqQuery where TModel : class, new()
    {
        /// <summary>
        /// Reads a LINQ query from its expression.
        /// </summary>
        /// <param name="expression">The query, from <see cref="IRepo.Query{TModel}"/> on.</param>
        /// <param name="terminal">The call that ends it when it isn't in the expression, as the Async methods pass it, otherwise <see langword="null"/>.</param>
        /// <param name="argument">The ending call's condition or value, when it has one.</param>
        /// <param name="graph">The members and relations of the models to read, from <see cref="IRepo.Query{TModel}"/>, or <see langword="null"/>.</param>
        /// <param name="temporal">What the query reads of the history of the models, from <see cref="IRepo.TemporalQuery{TModel}"/> or <see cref="IRepo.EventQuery{TModel}"/>, or <see langword="null"/> for their current state.</param>
        public LinqQuery(Expression expression, LinqTerminal? terminal, LambdaExpression? argument, Graph? graph = null, LinqTemporal? temporal = null)
            : base(typeof(TModel), typeof(TResult), expression, terminal, argument, graph, null, temporal)
        {

        }

        internal LinqQuery(Expression expression, LinqElements elements, LinqTerminal? terminal, LambdaExpression? argument)
            : base(typeof(TModel), typeof(TResult), expression, terminal, argument, null, elements)
        {

        }

        private protected override LinqQuery NumberedPart(Expression expression, string operation)
        {
            var part = new LinqQuery<TModel, object?>(expression, null, null);
            part.ReadAsNumberedPart(operation);
            return part;
        }
        private protected override LinqQuery SetPart(Expression expression)
        {
            //the elements are read as objects, the query's own result is a count where it ends in one
            var part = new LinqQuery<TModel, object?>(expression, null, null);
            part.ReadAsSetPart();
            return part;
        }

        internal override object CombineSet(IEnumerable left, IEnumerable right, bool caseSensitive)
        {
            var comparer = LinqElementComparer.Instance;
            var first = left.Cast<object?>();
            var second = right.Cast<object?>();
            var elements = Set!.Operation switch
            {
                LinqSetOperation.Union => first.Union(second, comparer),
                LinqSetOperation.Concat => first.Concat(second),
                LinqSetOperation.Intersect => first.Intersect(second, comparer),
                LinqSetOperation.Except => first.Except(second, comparer),
                _ => throw new NotSupportedException($"{Set.Operation} isn't a set operation"),
            };
            return EndCombined(elements, caseSensitive);
        }

        [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "A group is an array of the elements of the second query, which the query already reads.")]
        internal override object CombineJoin(IEnumerable left, IEnumerable? right, bool caseSensitive)
        {
            var join = Join!;
            if (join.Kind is not (LinqJoinKind.Inner or LinqJoinKind.Left or LinqJoinKind.Right or LinqJoinKind.Group))
                return CombinePosition(left.Cast<object?>().ToList(), right?.Cast<object?>().ToList(), caseSensitive);
            if (right is null)
                throw new InvalidOperationException("A join reads two queries");
            var leftKey = KeyFunction(join.LeftKey!, caseSensitive);
            var rightKey = KeyFunction(join.RightKey!, caseSensitive);
            var pair = PairFunction(join.Result!, join.LeftElement, join.RightElement, caseSensitive);

            //the elements of one query by their keys, a key of one part that is null matching none as in LINQ
            var comparer = new AggregateCommon.KeyComparer(caseSensitive);
            var leftByKey = join.Kind == LinqJoinKind.Right ? new Dictionary<object?[], List<object?>>(comparer) : null;
            var rightByKey = join.Kind == LinqJoinKind.Right ? null : new Dictionary<object?[], List<object?>>(comparer);
            var lookup = leftByKey ?? rightByKey!;
            foreach (var element in join.Kind == LinqJoinKind.Right ? left : right)
            {
                var key = (join.Kind == LinqJoinKind.Right ? leftKey : rightKey)(element);
                if (key is null)
                    continue;
                if (!lookup.TryGetValue(key, out var matches))
                {
                    matches = new List<object?>();
                    lookup.Add(key, matches);
                }
                matches.Add(element);
            }

            var elements = new List<object?>();
            if (join.Kind == LinqJoinKind.Right)
            {
                foreach (var element in right)
                {
                    var key = rightKey(element);
                    if (key is not null && lookup.TryGetValue(key, out var matches))
                    {
                        foreach (var match in matches)
                            elements.Add(pair(match, element));
                    }
                    else
                    {
                        elements.Add(pair(null, element));
                    }
                }
            }
            else if (join.Kind == LinqJoinKind.Group)
            {
                var groupType = join.RightElement.Type.GetGenericArguments()[0];
                foreach (var element in left)
                {
                    var key = leftKey(element);
                    List<object?>? matches = null;
                    _ = key is not null && lookup.TryGetValue(key, out matches);
                    var group = Array.CreateInstance(groupType, matches?.Count ?? 0);
                    for (var i = 0; i < group.Length; i++)
                        group.SetValue(matches![i], i);
                    elements.Add(pair(element, group));
                }
            }
            else
            {
                foreach (var element in left)
                {
                    var key = leftKey(element);
                    if (key is not null && lookup.TryGetValue(key, out var matches))
                    {
                        foreach (var match in matches)
                            elements.Add(pair(element, match));
                    }
                    else if (join.Kind == LinqJoinKind.Left)
                    {
                        elements.Add(pair(element, null));
                    }
                }
            }
            return EndCombined(elements, caseSensitive);
        }
        //an expression over two elements run on them read as objects; no element of one is its type's default, as LINQ gives
        private static Func<object?, object?, object?> PairFunction(Expression result, ParameterExpression left, ParameterExpression right, bool caseSensitive)
        {
            var first = Expression.Parameter(typeof(object), "first");
            var second = Expression.Parameter(typeof(object), "second");
            static Expression Element(ParameterExpression element, Type type) => type.IsValueType ? Expression.Condition(Expression.Equal(element, Expression.Constant(null)), Expression.Default(type), Expression.Convert(element, type)) : Expression.Convert(element, type);
            var body = LinqRebinder.RebindExpression(result, left, Element(first, left.Type));
            body = LinqRebinder.RebindExpression(body, right, Element(second, right.Type));
            return MemoryExpressionRewriter.Rewrite(Expression.Lambda<Func<object?, object?, object?>>(Expression.Convert(body, typeof(object)), first, second), caseSensitive).Compile();
        }

        //an operation by position over the elements of each query in their order
        private object CombinePosition(List<object?> left, List<object?>? right, bool caseSensitive)
        {
            var join = Join!;
            var elements = new List<object?>();
            Func<object?[]?, object>? tuple = join.TupleType is null ? null : TypeAnalyzer.GetTypeDetail(join.TupleType).GetConstructor(2).CreatorBoxed;
            switch (join.Kind)
            {
                case LinqJoinKind.Zip:
                    {
                        var pair = join.Result is null ? null : PairFunction(join.Result, join.LeftElement, join.RightElement, caseSensitive);
                        for (var i = 0; i < left.Count && i < right!.Count; i++)
                            elements.Add(pair is null ? tuple!([left[i], right[i]]) : pair(left[i], right[i]));
                        break;
                    }
                case LinqJoinKind.SequenceEqual:
                    //a pair that differs is all that is read
                    if (!left.SequenceEqual(right!, LinqElementComparer.Instance))
                        elements.Add(1);
                    break;
                case LinqJoinKind.Index:
                    {
                        var pair = join.Result is null ? null : PairFunction(join.Result, join.LeftElement, join.RightElement, caseSensitive);
                        var condition = join.Condition is null ? null : PairFunction(join.Condition, join.LeftElement, join.RightElement, caseSensitive);
                        for (var i = 0; i < left.Count; i++)
                        {
                            if (condition is not null && !(bool)condition(left[i], i)!)
                                continue;
                            elements.Add(pair is null ? tuple!([i, left[i]]) : pair(left[i], i));
                        }
                        break;
                    }
                case LinqJoinKind.While:
                    {
                        var condition = PairFunction(join.Condition!, join.LeftElement, join.RightElement, caseSensitive);
                        var failed = false;
                        for (var i = 0; i < left.Count; i++)
                        {
                            failed |= !(bool)condition(left[i], i)!;
                            if (failed == join.Skips)
                                elements.Add(left[i]);
                        }
                        break;
                    }
                case LinqJoinKind.Last:
                    if (join.At)
                    {
                        if (join.Count > 0 && join.Count <= left.Count)
                            elements.Add(left[left.Count - join.Count]);
                    }
                    else
                    {
                        elements.AddRange(join.Skips ? left.SkipLast(join.Count) : left.TakeLast(join.Count));
                    }
                    break;
                case LinqJoinKind.DistinctBy:
                case LinqJoinKind.UnionBy:
                    {
                        //a key of one part that is null is a key as any other here
                        var parts = AggregateCommon.GetKeyParts(join.LeftKey!);
                        var element = Expression.Parameter(typeof(object), "element");
                        var getters = new Func<object?, object?>[parts.Length];
                        for (var i = 0; i < parts.Length; i++)
                            getters[i] = MemoryExpressionRewriter.Rewrite(Expression.Lambda<Func<object?, object?>>(Expression.Convert(LinqRebinder.RebindExpression(parts[i], join.LeftKey!.Parameters[0], Expression.Convert(element, join.LeftKey.Parameters[0].Type)), typeof(object)), element), caseSensitive).Compile();
                        var seen = new HashSet<object?[]>(new AggregateCommon.KeyComparer(caseSensitive));
                        foreach (var item in join.Kind == LinqJoinKind.UnionBy ? left.Concat(right!) : left)
                        {
                            var key = new object?[getters.Length];
                            for (var i = 0; i < key.Length; i++)
                                key[i] = getters[i](item);
                            if (seen.Add(key))
                                elements.Add(item);
                        }
                        break;
                    }
                case LinqJoinKind.Append:
                    if (join.Prepends)
                        elements.Add(join.Value);
                    elements.AddRange(left);
                    if (!join.Prepends)
                        elements.Add(join.Value);
                    break;
            }
            return EndCombined(elements, caseSensitive);
        }

        //the parts of a key of an element, or null when a key of one part is null
        private static Func<object?, object?[]?> KeyFunction(LambdaExpression key, bool caseSensitive)
        {
            var parts = AggregateCommon.GetKeyParts(key);
            var element = Expression.Parameter(typeof(object), "element");
            var getters = new Func<object?, object?>[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                var part = LinqRebinder.RebindExpression(parts[i], key.Parameters[0], Expression.Convert(element, key.Parameters[0].Type));
                getters[i] = MemoryExpressionRewriter.Rewrite(Expression.Lambda<Func<object?, object?>>(Expression.Convert(part, typeof(object)), element), caseSensitive).Compile();
            }
            if (getters.Length == 1)
            {
                var getter = getters[0];
                return element => getter(element) is { } value ? [value] : null;
            }
            return element =>
            {
                var values = new object?[getters.Length];
                for (var i = 0; i < values.Length; i++)
                    values[i] = getters[i](element);
                return values;
            };
        }

        //the elements a set operation or a join gives, filtered, ordered, built from, paged, and ended as the query says
        private object EndCombined(IEnumerable<object?> elements, bool caseSensitive)
        {
            if (Select is not null && (ReadsElements || SelectedHaving is not null || Distinct))
                return CombineSelected(elements, caseSensitive);
            if (Having is not null)
            {
                var condition = ElementFunction(Having, caseSensitive);
                elements = elements.Where(x => (bool)condition(x)!);
            }
            if (Distinct)
                elements = elements.Distinct(LinqElementComparer.Instance);
            if (ResultOrder is not null && (ReadsElements || Skip.HasValue || Take.HasValue))
            {
                IOrderedEnumerable<object?>? ordered = null;
                foreach (var order in ResultOrder)
                {
                    var key = ElementFunction(order.Expression, caseSensitive);
                    if (ordered is null)
                        ordered = order.Descending ? elements.OrderByDescending(key) : elements.OrderBy(key);
                    else
                        ordered = order.Descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
                }
                elements = ordered!;
            }
            if (Skip.HasValue)
                elements = elements.Skip(Skip.Value);
            if (Take.HasValue)
                elements = elements.Take(Take.Value);
            switch (Terminal)
            {
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    return elements.LongCount();
                case LinqTerminal.Any:
                    return elements.Any();
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    {
                        var value = ElementFunction(Value!, caseSensitive);
                        var values = new List<object?>();
                        foreach (var element in elements)
                            values.Add(value(element));
                        var kind = Terminal switch
                        {
                            LinqTerminal.Sum => LinqColumnKind.Sum,
                            LinqTerminal.Min => LinqColumnKind.Min,
                            LinqTerminal.Max => LinqColumnKind.Max,
                            _ => LinqColumnKind.Average,
                        };
                        //an element taken as an object is aggregated as its own type
                        var type = Value!.Body is UnaryExpression { NodeType: ExpressionType.Convert } boxed && boxed.Type == typeof(object) ? boxed.Operand.Type : Value.ReturnType;
                        return AggregateCommon.Aggregate(kind, values, type)!;
                    }
                default:
                    return elements.Cast<TResult>().ToList();
            }
        }
        private object CombineSelected(IEnumerable<object?> combined, bool caseSensitive)
        {
            var select = ElementFunction(Select!, caseSensitive);
            var elements = combined.Select(x => new KeyValuePair<object?, object?>(x, select(x)));
            if (Having is not null)
            {
                var condition = ElementFunction(Having, caseSensitive);
                elements = elements.Where(x => (bool)condition(x.Key)!);
            }
            if (SelectedHaving is not null)
            {
                var condition = ElementFunction(SelectedHaving, caseSensitive);
                elements = elements.Where(x => (bool)condition(x.Value)!);
            }
            if (Distinct)
                elements = elements.DistinctBy(x => x.Value, LinqElementComparer.Instance);
            if ((ResultOrder is not null || SelectedOrder is not null) && (ReadsElements || Skip.HasValue || Take.HasValue))
            {
                IOrderedEnumerable<KeyValuePair<object?, object?>>? ordered = null;
                foreach (var order in ResultOrder ?? [])
                {
                    var key = ElementFunction(order.Expression, caseSensitive);
                    Func<KeyValuePair<object?, object?>, object?> pairKey = x => key(x.Key);
                    if (ordered is null)
                        ordered = order.Descending ? elements.OrderByDescending(pairKey) : elements.OrderBy(pairKey);
                    else
                        ordered = order.Descending ? ordered.ThenByDescending(pairKey) : ordered.ThenBy(pairKey);
                }
                foreach (var order in SelectedOrder ?? [])
                {
                    var key = ElementFunction(order.Expression, caseSensitive);
                    Func<KeyValuePair<object?, object?>, object?> pairKey = x => key(x.Value);
                    if (ordered is null)
                        ordered = order.Descending ? elements.OrderByDescending(pairKey) : elements.OrderBy(pairKey);
                    else
                        ordered = order.Descending ? ordered.ThenByDescending(pairKey) : ordered.ThenBy(pairKey);
                }
                elements = ordered!;
            }
            if (Skip.HasValue)
                elements = elements.Skip(Skip.Value);
            if (Take.HasValue)
                elements = elements.Take(Take.Value);
            return Terminal switch
            {
                LinqTerminal.Count or LinqTerminal.LongCount => elements.LongCount(),
                LinqTerminal.Any => elements.Any(),
                _ => elements.Select(x => x.Value).Cast<TResult>().ToList(),
            };
        }

        //a lambda over an element run on the element read as an object
        private static Func<object?, object?> ElementFunction(LambdaExpression lambda, bool caseSensitive)
        {
            var element = Expression.Parameter(typeof(object), "element");
            var rewritten = MemoryExpressionRewriter.Rewrite(lambda, caseSensitive);
            var body = Expression.Invoke(rewritten, Expression.Convert(element, lambda.Parameters[0].Type));
            return Expression.Lambda<Func<object?, object?>>(Expression.Convert(body, typeof(object)), element).Compile();
        }

        private protected override LambdaExpression ModelLambda(Expression body, ParameterExpression parameter)
            => Expression.Lambda<Func<TModel, object?>>(body.Type.IsValueType ? Expression.Convert(body, typeof(object)) : body, parameter);

        /// <summary>Initializes a query a provider makes to load models, with the parts it already has.</summary>
        /// <param name="where">The condition, or <see langword="null"/>.</param>
        /// <param name="order">The order, or <see langword="null"/>.</param>
        /// <param name="skip">The number of models to skip.</param>
        /// <param name="take">The maximum number of models.</param>
        /// <param name="graph">The members to read, or <see langword="null"/> for every member.</param>
        /// <param name="terminal">The call that ends the query.</param>
        public LinqQuery(LambdaExpression? where, LinqOrder[]? order, int? skip, int? take, Graph? graph, LinqTerminal terminal)
            : base(typeof(TModel), typeof(TResult), where, order, skip, take, graph, terminal)
        {

        }

        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => engine.ExecuteLinq<TModel, TResult>(this, modelDetail);
        internal override async Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken) => await engine.ExecuteLinqAsync<TModel, TResult>(this, modelDetail, cancellationToken);
        internal override object Stream(ITransactStoreEngine engine, ModelDetail modelDetail) => engine.ExecuteLinqStream<TModel, TResult>(this, modelDetail);
        internal override object StreamFrom(Func<object?> open, Action? opened) => StreamOpened(open, opened);
        private static IEnumerable<TResult> StreamOpened(Func<object?> open, Action? opened)
        {
            IEnumerator<TResult> elements;
            bool any;
            try
            {
                elements = ((IEnumerable<TResult>)open()!).GetEnumerator();
                try
                {
                    any = elements.MoveNext();
                }
                catch
                {
                    elements.Dispose();
                    throw;
                }
            }
            finally
            {
                opened?.Invoke();
            }
            using (elements)
            {
                while (any)
                {
                    yield return elements.Current;
                    any = elements.MoveNext();
                }
            }
        }
        internal override object StreamAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken) => engine.ExecuteLinqStreamAsync<TModel, TResult>(this, modelDetail, cancellationToken);
        internal override object StreamFromAsync(Func<CancellationToken, Task<object?>> open, Action? opened, CancellationToken cancellationToken) => StreamOpenedAsync(open, opened, cancellationToken);
        private static async IAsyncEnumerable<TResult> StreamOpenedAsync(Func<CancellationToken, Task<object?>> open, Action? opened, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IAsyncEnumerator<TResult>? stream = null;
            IEnumerator<TResult>? list = null;
            bool any;
            try
            {
                var source = await open(cancellationToken);
                if (source is IAsyncEnumerable<TResult> elements)
                {
                    stream = elements.GetAsyncEnumerator(cancellationToken);
                    any = await stream.MoveNextAsync();
                }
                else
                {
                    list = ((IEnumerable<TResult>)source!).GetEnumerator();
                    any = list.MoveNext();
                }
            }
            catch
            {
                if (stream is not null)
                    await stream.DisposeAsync();
                list?.Dispose();
                throw;
            }
            finally
            {
                opened?.Invoke();
            }

            if (stream is not null)
            {
                try
                {
                    while (any)
                    {
                        yield return stream.Current;
                        any = await stream.MoveNextAsync();
                    }
                }
                finally
                {
                    await stream.DisposeAsync();
                }
            }
            else
            {
                try
                {
                    while (any)
                    {
                        yield return list!.Current;
                        cancellationToken.ThrowIfCancellationRequested();
                        any = list.MoveNext();
                    }
                }
                finally
                {
                    list!.Dispose();
                }
            }
        }

        internal override object? ExecuteRead(IEnumerable<object> models, bool caseSensitive)
        {
            if (Combines || !ReturnsModels || Graph is not null || Distinct || typeof(TResult) != typeof(TModel))
                return Execute(models, caseSensitive);
            var source = (IEnumerable<TModel>)models;
            if (Where is not null)
                source = source.Where(MemoryExpressionRewriter.Rewrite((Expression<Func<TModel, bool>>)Where, caseSensitive).Compile());
            source = Ordered(source, Order, caseSensitive);
            if (Skip.HasValue)
                source = source.Skip(Skip.Value);
            if (Take.HasValue)
                source = source.Take(Take.Value);
            var results = source.ToList();
            //relations aren't asked for, so those read for the condition or order or kept with the stored model aren't given
            var related = ModelAnalyzer.GetModel(typeof(TModel)).RelatedMembers;
            if (related.Count > 0)
            {
                foreach (var model in results)
                {
                    foreach (var member in related)
                        member.SetterBoxed(model, null);
                }
            }
            return results;
        }

        /// <inheritdoc/>
        //each column and relation of an element read from the model, as the memory engine and an event store run the query
        public override object? Execute(IEnumerable<object> models, bool caseSensitive)
        {
            if (Set is not null)
                return CombineSet((IEnumerable)Set.Left.Execute(models, caseSensitive)!, Set.ReadRight(), caseSensitive);
            if (Join is not null)
                return CombineJoin((IEnumerable)Join.Left.Execute(models, caseSensitive)!, Join.ReadRight(), caseSensitive);
            var source = (IEnumerable<TModel>)models;
            if (Where is not null)
                source = source.Where(MemoryExpressionRewriter.Rewrite((Expression<Func<TModel, bool>>)Where, caseSensitive).Compile());
            //a count or aggregate of a page reads the rows of the page
            if (!ReadsElements && (Skip.HasValue || Take.HasValue))
            {
                source = Ordered(source, Order, caseSensitive);
                if (Skip.HasValue)
                    source = source.Skip(Skip.Value);
                if (Take.HasValue)
                    source = source.Take(Take.Value);
            }
            switch (Terminal)
            {
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    if (Distinct)
                        break;
                    return source.LongCount();
                case LinqTerminal.Any:
                    return source.Any();
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    {
                        var getter = AggregateCommon.CompileBoxed<TModel>(Value!.Body, Value.Parameters[0], caseSensitive);
                        var values = new List<object?>();
                        foreach (var model in source)
                            values.Add(getter(model));
                        var kind = Terminal switch
                        {
                            LinqTerminal.Sum => LinqColumnKind.Sum,
                            LinqTerminal.Min => LinqColumnKind.Min,
                            LinqTerminal.Max => LinqColumnKind.Max,
                            _ => LinqColumnKind.Average,
                        };
                        return AggregateCommon.Aggregate(kind, values, Value.ReturnType);
                    }
            }

            var plan = LinqPlan.Read(this, ModelAnalyzer.GetModel(typeof(TModel)));
            if (GroupBy is not null)
                return Groups(plan, source, caseSensitive);

            if (ReadsElements)
            {
                //rows numbered for an operation by position come in the order they are numbered in
                source = Ordered(source, Order ?? NumberOrder, caseSensitive);
                if (Skip.HasValue)
                    source = source.Skip(Skip.Value);
                if (Take.HasValue)
                    source = source.Take(Take.Value);
            }

            var getters = new Func<TModel, object?>[plan.Columns.Count + plan.Relations.Count];
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                var column = plan.Columns[i];
                if (column.Joined || column.Kind == LinqColumnKind.RowNumber)
                    getters[i] = static _ => null;
                else if (column.Member is not null)
                    getters[i] = column.Member.GetterBoxed;
                else
                    getters[i] = AggregateCommon.CompileBoxed<TModel>(column.Expression!, column.Parameter!, caseSensitive);
            }
            for (var r = 0; r < plan.Relations.Count; r++)
                getters[plan.Columns.Count + r] = plan.Relations[r].Column!.Member is { } relation ? relation.GetterBoxed : AggregateCommon.CompileBoxed<TModel>(plan.Relations[r].Column!.Expression!, plan.Relations[r].Column!.Parameter!, caseSensitive);
            var seen = Distinct ? new HashSet<object?[]>(new AggregateCommon.KeyComparer(caseSensitive)) : null;
            var results = new List<TResult>();
            foreach (var model in source)
            {
                var row = new object?[getters.Length];
                for (var i = 0; i < row.Length; i++)
                    row[i] = getters[i](model);
                if (seen is not null && !seen.Add(row))
                    continue;
                if (Terminal is LinqTerminal.Count or LinqTerminal.LongCount)
                    continue;
                results.Add((TResult)plan.Shape!.Build(row, null)!);
            }
            if (Terminal is LinqTerminal.Count or LinqTerminal.LongCount)
                return (long)seen!.Count;
            return results;
        }

        private object Groups(LinqPlan plan, IEnumerable<TModel> source, bool caseSensitive)
        {
            var parts = plan.KeyParts!;
            var getParts = new Func<TModel, object?>[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                getParts[i] = AggregateCommon.CompileBoxed<TModel>(parts[i], plan.KeyParameter!, caseSensitive);
            var getValues = new Func<TModel, object?>?[plan.Columns.Count];
            for (var i = 0; i < getValues.Length; i++)
            {
                if (plan.Columns[i].Expression is not null)
                    getValues[i] = AggregateCommon.CompileBoxed<TModel>(plan.Columns[i].Expression!, plan.Columns[i].Parameter!, caseSensitive);
            }

            var groups = new Dictionary<object?[], List<TModel>>(new AggregateCommon.KeyComparer(caseSensitive));
            var keys = new List<object?[]>();
            foreach (var model in source)
            {
                var key = new object?[parts.Length];
                for (var i = 0; i < key.Length; i++)
                    key[i] = getParts[i](model);
                if (!groups.TryGetValue(key, out var group))
                {
                    group = new List<TModel>();
                    groups.Add(key, group);
                    keys.Add(key);
                }
                group.Add(model);
            }

            var built = new List<TResult>(keys.Count);
            foreach (var key in keys)
            {
                var group = groups[key];
                var row = new object?[plan.Columns.Count];
                for (var c = 0; c < row.Length; c++)
                {
                    var column = plan.Columns[c];
                    switch (column.Kind)
                    {
                        case LinqColumnKind.KeyPart:
                            row[c] = key[column.KeyPart];
                            break;
                        case LinqColumnKind.Count:
                            row[c] = (long)group.Count;
                            break;
                        case LinqColumnKind.CountIf:
                            {
                                var count = 0L;
                                foreach (var model in group)
                                    if ((bool)getValues[c]!(model)!)
                                        count++;
                                row[c] = count;
                                break;
                            }
                        case LinqColumnKind.Join:
                            {
                                var joined = new List<string?>(group.Count);
                                foreach (var model in group)
                                    joined.Add(getValues[c]!(model)?.ToString());
                                row[c] = String.Join(column.Separator, joined);
                                break;
                            }
                        default:
                            {
                                var values = new List<object?>(group.Count);
                                foreach (var model in group)
                                    values.Add(getValues[c]!(model));
                                row[c] = AggregateCommon.Aggregate(column.Kind, values, column.Expression!.Type);
                                break;
                            }
                    }
                }
                built.Add((TResult)plan.Shape!.Build(row, null)!);
            }

            IEnumerable<TResult> results = built;
            if (Having is not null)
                results = results.Where(MemoryExpressionRewriter.Rewrite((Expression<Func<TResult, bool>>)Having, caseSensitive).Compile());
            results = Ordered(results, ResultOrder, caseSensitive);
            if (Skip.HasValue)
                results = results.Skip(Skip.Value);
            if (Take.HasValue)
                results = results.Take(Take.Value);
            return results.ToList();
        }

        private static IEnumerable<T> Ordered<T>(IEnumerable<T> source, LinqOrder[]? orders, bool caseSensitive)
        {
            if (orders is null)
                return source;
            IOrderedEnumerable<T>? ordered = null;
            foreach (var order in orders)
            {
                var lambda = MemoryExpressionRewriter.Rewrite(order.Expression, caseSensitive);
                var key = Expression.Lambda<Func<T, object?>>(Expression.Convert(lambda.Body, typeof(object)), lambda.Parameters).Compile();
                if (ordered is null)
                    ordered = order.Descending ? source.OrderByDescending(key, Comparer<object?>.Default) : source.OrderBy(key, Comparer<object?>.Default);
                else
                    ordered = order.Descending ? ordered.ThenByDescending(key, Comparer<object?>.Default) : ordered.ThenBy(key, Comparer<object?>.Default);
            }
            return ordered ?? source;
        }
    }
}
