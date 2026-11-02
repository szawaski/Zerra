// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A LINQ query of <typeparamref name="TModel"/> giving elements or a value of <typeparamref name="TResult"/>, which runs on an engine or over loaded models with both types known.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TResult">The type of the elements, or of the value the query ends in.</typeparam>
    public sealed class LinqQuery<TModel, TResult> : LinqQuery where TModel : class, new()
    {
        /// <summary>
        /// Reads a LINQ query from its expression.
        /// </summary>
        /// <param name="expression">The query, from <see cref="IRepo.Query{TModel}"/> on.</param>
        /// <param name="terminal">The call that ends it when it isn't in the expression, as the Async methods pass it, otherwise <see langword="null"/>.</param>
        /// <param name="argument">The ending call's condition or value, when it has one.</param>
        /// <param name="graph">The members and relations of the models to read, from <see cref="IRepo.Query{TModel}"/>, or <see langword="null"/>.</param>
        public LinqQuery(Expression expression, LinqTerminal? terminal, LambdaExpression? argument, Graph? graph = null)
            : base(typeof(TModel), typeof(TResult), expression, terminal, argument, graph)
        {

        }

        internal LinqQuery(Expression expression, LinqElements elements, LinqTerminal? terminal, LambdaExpression? argument)
            : base(typeof(TModel), typeof(TResult), expression, terminal, argument, null, elements)
        {

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
        public LinqQuery(LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, LinqTerminal terminal)
            : base(typeof(TModel), typeof(TResult), where, order, skip, take, graph, terminal)
        {

        }

        internal override object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => engine.ExecuteLinq<TModel, TResult>(this, modelDetail);
        internal override async Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken) => await engine.ExecuteLinqAsync<TModel, TResult>(this, modelDetail, cancellationToken);

        /// <inheritdoc/>
        //each column and relation of an element read from the model, as the memory engine and an event store run the query
        public override object? Execute(IEnumerable<object> models, bool caseSensitive)
        {
            var source = (IEnumerable<TModel>)models;
            if (Where is not null)
                source = source.Where(MemoryExpressionRewriter.Rewrite((Expression<Func<TModel, bool>>)Where, caseSensitive).Compile());
            //a count or calculation of a page reads the rows of the page
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
                        var getter = CalculationCommon.CompileBoxed<TModel>(Value!.Body, Value.Parameters[0], caseSensitive);
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
                        return CalculationCommon.Aggregate(kind, values, Value.ReturnType);
                    }
            }

            var plan = LinqPlan.Read(this, ModelAnalyzer.GetModel(typeof(TModel)));
            if (GroupBy is not null)
                return Groups(plan, source, caseSensitive);

            if (ReadsElements)
            {
                source = Ordered(source, Order, caseSensitive);
                if (Skip.HasValue)
                    source = source.Skip(Skip.Value);
                if (Take.HasValue)
                    source = source.Take(Take.Value);
            }

            var getters = new Func<TModel, object?>[plan.Columns.Count + plan.Relations.Count];
            for (var i = 0; i < plan.Columns.Count; i++)
            {
                var column = plan.Columns[i];
                if (column.Joined)
                    getters[i] = static _ => null;
                else if (column.Member is not null)
                    getters[i] = column.Member.GetterBoxed;
                else
                    getters[i] = CalculationCommon.CompileBoxed<TModel>(column.Expression!, column.Parameter!, caseSensitive);
            }
            for (var r = 0; r < plan.Relations.Count; r++)
                getters[plan.Columns.Count + r] = plan.Relations[r].Column!.Member is { } relation ? relation.GetterBoxed : CalculationCommon.CompileBoxed<TModel>(plan.Relations[r].Column!.Expression!, plan.Relations[r].Column!.Parameter!, caseSensitive);
            var seen = Distinct ? new HashSet<object?[]>(new CalculationCommon.KeyComparer(caseSensitive)) : null;
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
                getParts[i] = CalculationCommon.CompileBoxed<TModel>(parts[i], plan.KeyParameter!, caseSensitive);
            var getValues = new Func<TModel, object?>?[plan.Columns.Count];
            for (var i = 0; i < getValues.Length; i++)
            {
                if (plan.Columns[i].Expression is not null)
                    getValues[i] = CalculationCommon.CompileBoxed<TModel>(plan.Columns[i].Expression!, plan.Columns[i].Parameter!, caseSensitive);
            }

            var groups = new Dictionary<object?[], List<TModel>>(new CalculationCommon.KeyComparer(caseSensitive));
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
                                row[c] = CalculationCommon.Aggregate(column.Kind, values, column.Expression!.Type);
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

        private static IEnumerable<T> Ordered<T>(IEnumerable<T> source, QueryOrder? orders, bool caseSensitive)
        {
            if (orders is null)
                return source;
            IOrderedEnumerable<T>? ordered = null;
            foreach (var order in orders.OrderExpressions)
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
