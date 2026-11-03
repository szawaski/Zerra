// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// A LINQ query from <see cref="IRepo.Query{TModel}"/>, read from its calls in order: the conditions, the order, the paging, the projection, the grouping, and the call that ends it.
    /// It runs through the model's provider, where a layer may add to or change the conditions, and the store converts it in one pass.
    /// </summary>
    public abstract class LinqQuery
    {
        /// <summary>Gets the CLR type of the model being queried.</summary>
        public Type ModelType { get; }
        /// <summary>Gets the type of the elements, or of the value the query ends in.</summary>
        public Type ResultType { get; }
        /// <summary>Gets or sets the condition on the models, the Where calls as one. A layer swaps its own condition in for the call and puts the original back.</summary>
        public LambdaExpression? Where { get; set; }
        /// <summary>Gets the order of the models, the OrderBy and ThenBy calls as one, or <see langword="null"/>.</summary>
        public QueryOrder? Order { get; private set; }
        /// <summary>Gets the number of models to skip.</summary>
        public int? Skip { get; private set; }
        /// <summary>Gets the maximum number of models, or of groups, to take.</summary>
        public int? Take { get; private set; }
        /// <summary>Gets the projection building each element from a model or a group, or <see langword="null"/> for the models.</summary>
        public LambdaExpression? Select { get; internal set; }
        /// <summary>Gets the key the models are grouped by, or <see langword="null"/>.</summary>
        public LambdaExpression? GroupBy { get; private set; }
        /// <summary>Gets the condition on the results of a grouped query, the Where calls after its Select as one.</summary>
        public LambdaExpression? Having { get; private set; }
        /// <summary>Gets the order of the results of a grouped query, the OrderBy and ThenBy calls after its Select as one, or <see langword="null"/>.</summary>
        public QueryOrder? ResultOrder { get; private set; }
        /// <summary>Gets the condition on the elements a <see cref="Select"/> after a set operation builds, the Where calls after it as one.</summary>
        public LambdaExpression? SelectedHaving { get; private set; }
        /// <summary>Gets the order of the elements a <see cref="Select"/> after a set operation builds, after <see cref="ResultOrder"/>, or <see langword="null"/>.</summary>
        public QueryOrder? SelectedOrder { get; private set; }
        /// <summary>Gets whether an element the same as another counts once.</summary>
        public bool Distinct { get; private set; }
        /// <summary>Gets the call that ends the query.</summary>
        public LinqTerminal Terminal { get; private set; }
        /// <summary>Gets the value a Sum, Min, Max, or Average calculates.</summary>
        public LambdaExpression? Value { get; private set; }
        /// <summary>Gets whether an Any came from an All, so its answer is turned round.</summary>
        public bool Negated { get; private set; }

        /// <summary>Gets the members and relations of the models to read, or <see langword="null"/> for every member and no relations.</summary>
        public Graph? Graph { get; }
        //a graph given to the query reads its relations with the rows; a provider loading models reads their relations itself
        internal bool GraphRelations { get; }

        /// <summary>Gets whether the query gives models rather than projections or values.</summary>
        public bool ReturnsModels => Select is null && GroupBy is null && KeyMember is null && ReadsElements;
        /// <summary>Gets whether the query reads its elements, rather than a count, whether there is any, or a calculation.</summary>
        public bool ReadsElements => Terminal is LinqTerminal.Sequence or LinqTerminal.First or LinqTerminal.FirstOrDefault or LinqTerminal.Single or LinqTerminal.SingleOrDefault or LinqTerminal.Last or LinqTerminal.LastOrDefault or LinqTerminal.ElementAt or LinqTerminal.ElementAtOrDefault;
        /// <summary>Gets whether the condition looks in another query.</summary>
        public bool HasSubquery { get; private set; }
        /// <summary>Gets the two queries a set operation combines into the elements, whose condition is <see cref="Having"/> and order <see cref="ResultOrder"/>, built from by <see cref="Select"/> with its condition <see cref="SelectedHaving"/> and order <see cref="SelectedOrder"/>, then paged by <see cref="Skip"/> and <see cref="Take"/>; <see langword="null"/> for none.</summary>
        public LinqSet? Set { get; private set; }
        internal ModelMemberDetail? KeyMember { get; private set; }
        /// <summary>Gets whether the projection picks one value rather than building an object.</summary>
        public bool IsScalarSelect => Select is not null && Select.Body is not MemberInitExpression && Select.Body is not NewExpression;
        private protected LinqQuery(Type modelType, Type resultType, Expression expression, LinqTerminal? terminal, LambdaExpression? argument, Graph? graph, LinqElements? elements = null)
        {
            this.ModelType = modelType;
            this.ResultType = resultType;
            var calls = new List<MethodCallExpression>();
            var node = expression;
            while (node is MethodCallExpression call && call != elements?.Root)
            {
                calls.Add(call);
                if (call.Arguments.Count == 0)
                    break;
                node = call.Arguments[0];
            }
            if (elements is not null)
            {
                HasSubquery = true;
                Where = elements.Where is null ? elements.Condition : AppendAnd(elements.Condition, elements.Where);
                if (elements.Orders is not null)
                    Order = new ProjectionOrder(elements.Orders);
                Select = elements.Select ?? (elements.SelectBody is null ? null : ModelLambda(elements.SelectBody, elements.SelectParameter!));
            }
            else if (node is not ConstantExpression { Value: IQueryable })
            {
                throw new NotSupportedException("A repository query starts with IRepo.Query");
            }
            //a set operation combines the query before it with another, so only the ending follows it
            for (var i = 0; i < calls.Count; i++)
            {
                if (calls[i].Method.DeclaringType != typeof(System.Linq.Queryable) || SetOperation(calls[i].Method.Name) is not { } operation)
                    continue;
                if (elements is not null || graph is not null)
                    throw new NotSupportedException($"{calls[i].Method.Name} combines queries without a graph or SelectMany");
                Set = ReadSet(operation, calls[i]);
                for (var j = i - 1; j >= 0; j--)
                    EndSet(calls[j]);
                if (terminal.HasValue)
                    EndSetTerminal(terminal.Value, argument);
                return;
            }
            for (var i = calls.Count - 1; i >= 0; i--)
                Apply(calls[i]);
            if (terminal.HasValue)
                End(terminal.Value, argument);
            if (graph is not null)
            {
                if (Select is not null || GroupBy is not null)
                    throw new NotSupportedException("A graph and a Select both say what a query reads, so a query has one or the other");
                this.Graph = new Graph(graph);
                this.GraphRelations = true;
            }
        }

        //a query a provider makes to load models, with the parts it already has
        private protected LinqQuery(Type modelType, Type resultType, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph, LinqTerminal terminal)
        {
            this.ModelType = modelType;
            this.ResultType = resultType;
            this.Where = where;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
            this.Graph = graph;
            this.Terminal = terminal;
            //one row says what is first, two whether there is a single one; a model query picks from what its provider gives
            if (this is LinqModelQuery)
                return;
            if (terminal is LinqTerminal.First or LinqTerminal.FirstOrDefault)
                this.Take = Math.Min(take ?? 1, 1);
            else if (terminal is LinqTerminal.Single or LinqTerminal.SingleOrDefault)
                this.Take = Math.Min(take ?? 2, 2);
        }

        internal abstract object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail);
        internal abstract Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken);
        internal abstract object Stream(ITransactStoreEngine engine, ModelDetail modelDetail);
        //opened runs once the first element is read, so a layer puts back what it changed for the statement
        internal abstract object StreamFrom(Func<object?> open, Action? opened);
        internal abstract object StreamAsync(ITransactStoreEngine engine, ModelDetail modelDetail, CancellationToken cancellationToken);
        internal abstract object StreamFromAsync(Func<CancellationToken, Task<object?>> open, Action? opened, CancellationToken cancellationToken);
        /// <summary>
        /// Runs the query over models already loaded, as the memory engine and an event store do, with its result types; the models have the relations the query reads.
        /// </summary>
        /// <param name="models">The models.</param>
        /// <param name="caseSensitive">Whether string comparisons are case sensitive.</param>
        /// <returns>The elements as a list of the result type, or the value boxed: a long count, a bool, or a calculation.</returns>
        public abstract object? Execute(IEnumerable<object> models, bool caseSensitive);

        private void Apply(MethodCallExpression call)
        {
            if (call.Method.DeclaringType != typeof(System.Linq.Queryable))
                throw new NotSupportedException($"{call.Method.Name} isn't supported in a repository query");
            LambdaExpression? lambda = null;
            if (call.Arguments.Count > 1)
            {
                var argument = call.Arguments[1];
                if (argument.NodeType == ExpressionType.Quote)
                    argument = ((UnaryExpression)argument).Operand;
                lambda = argument as LambdaExpression;
                if (lambda is not null && lambda.Parameters.Count != 1)
                    throw new NotSupportedException($"{call.Method.Name} with more than the element isn't supported in a repository query");
            }
            if (call.Arguments.Count > 2)
                throw new NotSupportedException($"{call.Method.Name} with that many arguments isn't supported in a repository query");

            switch (call.Method.Name)
            {
                case "Where":
                    Condition(lambda ?? throw new NotSupportedException("Where takes a condition"), null);
                    break;
                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    {
                        if (lambda is null)
                            throw new NotSupportedException($"{call.Method.Name} takes the key to order by");
                        var grouped = GroupBy is not null && Select is not null;
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException("OrderBy after Skip or Take isn't supported, order before paging");
                        if (!grouped && Select is not null)
                            lambda = ModelLambda(LinqSelectRewriter.Rewrite(lambda, Select), Select.Parameters[0]);
                        var expression = new ProjectionOrderExpression(lambda, call.Method.Name is "OrderByDescending" or "ThenByDescending");
                        var order = grouped ? ResultOrder : Order;
                        if (order is null || call.Method.Name is "OrderBy" or "OrderByDescending")
                        {
                            order = new ProjectionOrder([expression]);
                        }
                        else
                        {
                            var expressions = new OrderExpression[order.OrderExpressions.Length + 1];
                            order.OrderExpressions.CopyTo(expressions, 0);
                            expressions[^1] = expression;
                            order = new ProjectionOrder(expressions);
                        }
                        if (grouped)
                            ResultOrder = order;
                        else
                            Order = order;
                        break;
                    }
                case "Skip":
                    if (Take.HasValue)
                        throw new NotSupportedException("Skip after Take isn't supported, skip before taking");
                    Skip = (Skip ?? 0) + Count(call.Arguments[1]);
                    break;
                case "Take":
                    {
                        var count = Count(call.Arguments[1]);
                        Take = Take.HasValue ? Math.Min(Take.Value, count) : count;
                        break;
                    }
                case "Select":
                    if (Select is not null)
                        throw new NotSupportedException("A repository query has one Select");
                    if (lambda is null)
                        throw new NotSupportedException("Select takes what to build from each element");
                    if (lambda.Body != lambda.Parameters[0])
                    {
                        Select = LinqSubquery.Rewrite(lambda, out var selectFound);
                        HasSubquery |= selectFound;
                    }
                    break;
                case "GroupBy":
                    if (Select is not null || GroupBy is not null || Order is not null || Skip.HasValue || Take.HasValue)
                        throw new NotSupportedException("GroupBy comes after the conditions and before a Select of each group");
                    GroupBy = lambda ?? throw new NotSupportedException("GroupBy takes the key to group by");
                    break;
                case "Distinct":
                    if (lambda is not null)
                        throw new NotSupportedException("Distinct with a comparer isn't supported in a repository query");
                    if (Skip.HasValue || Take.HasValue)
                        throw new NotSupportedException("Distinct after Skip or Take isn't supported, take the different elements after them");
                    Distinct = true;
                    break;
                case "First":
                    End(LinqTerminal.First, lambda);
                    break;
                case "FirstOrDefault":
                    End(LinqTerminal.FirstOrDefault, lambda);
                    break;
                case "Single":
                    End(LinqTerminal.Single, lambda);
                    break;
                case "SingleOrDefault":
                    End(LinqTerminal.SingleOrDefault, lambda);
                    break;
                case "Any":
                    End(LinqTerminal.Any, lambda);
                    break;
                case "Count":
                    End(LinqTerminal.Count, lambda);
                    break;
                case "LongCount":
                    End(LinqTerminal.LongCount, lambda);
                    break;
                case "All":
                    //whether every element meets a condition is whether none fails it
                    Negated = true;
                    End(LinqTerminal.Any, Expression.Lambda(lambda!.Type, Expression.Not(lambda.Body), lambda.Parameters));
                    break;
                case "Last":
                    End(LinqTerminal.Last, lambda);
                    break;
                case "LastOrDefault":
                    End(LinqTerminal.LastOrDefault, lambda);
                    break;
                case "ElementAt":
                    EndAt(LinqTerminal.ElementAt, Count(call.Arguments[1]));
                    break;
                case "ElementAtOrDefault":
                    EndAt(LinqTerminal.ElementAtOrDefault, Count(call.Arguments[1]));
                    break;
                case "Reverse":
                    Reverse(call.Method.Name);
                    break;
                case "Contains":
                    EndContains(call.Arguments[1]);
                    break;
                case "Sum":
                    End(LinqTerminal.Sum, lambda);
                    break;
                case "Min":
                    End(LinqTerminal.Min, lambda);
                    break;
                case "Max":
                    End(LinqTerminal.Max, lambda);
                    break;
                case "Average":
                    End(LinqTerminal.Average, lambda);
                    break;
                default:
                    throw new NotSupportedException($"{call.Method.Name} isn't supported in a repository query");
            }
        }

        private void End(LinqTerminal terminal, LambdaExpression? argument)
        {
            switch (terminal)
            {
                case LinqTerminal.Last:
                case LinqTerminal.LastOrDefault:
                    Terminal = terminal;
                    if (argument is not null)
                        Condition(argument, terminal);
                    Reverse(terminal.ToString());
                    Take = 1;
                    break;
                case LinqTerminal.First:
                case LinqTerminal.FirstOrDefault:
                case LinqTerminal.Single:
                case LinqTerminal.SingleOrDefault:
                    {
                        Terminal = terminal;
                        if (argument is not null)
                            Condition(argument, terminal);
                        //one row says what is first, two whether there is a single one
                        var rows = terminal is LinqTerminal.Single or LinqTerminal.SingleOrDefault ? 2 : 1;
                        Take = Take.HasValue ? Math.Min(Take.Value, rows) : rows;
                        break;
                    }
                case LinqTerminal.Any:
                case LinqTerminal.Count:
                case LinqTerminal.LongCount:
                    Terminal = terminal;
                    if (argument is not null)
                        Condition(argument, terminal);
                    break;
                case LinqTerminal.Sum:
                case LinqTerminal.Min:
                case LinqTerminal.Max:
                case LinqTerminal.Average:
                    Terminal = terminal;
                    if (GroupBy is not null)
                        throw new NotSupportedException($"{terminal} after GroupBy isn't supported, calculate it in the Select of each group");
                    if (argument is null)
                    {
                        if (!IsScalarSelect || Select!.ReturnType == typeof(object))
                            throw new NotSupportedException($"{terminal} takes the value to calculate");
                        Value = Select;
                        Select = null;
                    }
                    else
                    {
                        if (Select is not null)
                            throw new NotSupportedException($"{terminal} after Select isn't supported, calculate the value of the models");
                        Value = argument;
                    }
                    break;
                default:
                    throw new NotSupportedException($"{terminal} isn't supported in a repository query");
            }
            if (Terminal is LinqTerminal.Count or LinqTerminal.LongCount or LinqTerminal.Any)
            {
                if (GroupBy is not null)
                    throw new NotSupportedException($"{terminal} after GroupBy isn't supported, read the groups");
            }
        }

        //a lambda over the model of what isn't known at compile time; only its body and parameter are read
        private protected abstract LambdaExpression ModelLambda(Expression body, ParameterExpression parameter);

        private static LinqSetOperation? SetOperation(string name) => name switch
        {
            "Union" => LinqSetOperation.Union,
            "Concat" => LinqSetOperation.Concat,
            "Intersect" => LinqSetOperation.Intersect,
            "Except" => LinqSetOperation.Except,
            _ => null,
        };
        private LinqSet ReadSet(LinqSetOperation operation, MethodCallExpression call)
        {
            if (call.Arguments.Count != 2)
                throw new NotSupportedException($"{call.Method.Name} with a comparer isn't supported in a repository query, elements compare by their values");
            var left = SetPart(call.Arguments[0]);
            object? source;
            try
            {
                source = Expression.Lambda<Func<object?>>(Expression.Convert(call.Arguments[1], typeof(object))).Compile()();
            }
            catch (InvalidOperationException ex)
            {
                throw new NotSupportedException($"The query {call.Method.Name} combines with is built from itself alone", ex);
            }
            if (source is not IQueryable { Provider: RepoQueryProvider provider } queryable)
                throw new NotSupportedException($"{call.Method.Name} combines queries from IRepo.Query");
            return provider.ReadSet(operation, left, queryable.Expression);
        }
        //what follows a set is of its elements, as a grouped query's having and order are of its results
        private void EndSet(MethodCallExpression call)
        {
            if (call.Method.DeclaringType != typeof(System.Linq.Queryable) || call.Arguments.Count > 2)
                throw new NotSupportedException($"{call.Method.Name} after a set operation isn't supported in a repository query");
            LambdaExpression? lambda = null;
            if (call.Arguments.Count == 2)
            {
                var argument = call.Arguments[1];
                if (argument.NodeType == ExpressionType.Quote)
                    argument = ((UnaryExpression)argument).Operand;
                lambda = argument as LambdaExpression;
            }
            switch (call.Method.Name)
            {
                case "Where":
                    SetCondition(lambda ?? throw new NotSupportedException("Where takes a condition"));
                    return;
                case "Select":
                    if (Select is not null)
                        throw new NotSupportedException("A repository query has one Select");
                    Select = lambda ?? throw new NotSupportedException("Select takes what to build from each element");
                    return;
                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    {
                        if (lambda is null)
                            throw new NotSupportedException($"{call.Method.Name} takes the key to order by");
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException("OrderBy after Skip or Take isn't supported, order before paging");
                        var expression = new ProjectionOrderExpression(lambda, call.Method.Name is "OrderByDescending" or "ThenByDescending");
                        var restart = call.Method.Name is "OrderBy" or "OrderByDescending";
                        if (Select is not null)
                        {
                            if (restart || (ResultOrder is null && SelectedOrder is null))
                            {
                                ResultOrder = null;
                                SelectedOrder = new ProjectionOrder([expression]);
                            }
                            else
                            {
                                var selected = SelectedOrder?.OrderExpressions ?? [];
                                var appended = new OrderExpression[selected.Length + 1];
                                selected.CopyTo(appended, 0);
                                appended[^1] = expression;
                                SelectedOrder = new ProjectionOrder(appended);
                            }
                            return;
                        }
                        if (ResultOrder is null || restart)
                        {
                            ResultOrder = new ProjectionOrder([expression]);
                        }
                        else
                        {
                            var expressions = new OrderExpression[ResultOrder.OrderExpressions.Length + 1];
                            ResultOrder.OrderExpressions.CopyTo(expressions, 0);
                            expressions[^1] = expression;
                            ResultOrder = new ProjectionOrder(expressions);
                        }
                        return;
                    }
                case "Skip":
                    if (Take.HasValue)
                        throw new NotSupportedException("Skip after Take isn't supported, skip before taking");
                    Skip = (Skip ?? 0) + Count(call.Arguments[1]);
                    return;
                case "Take":
                    {
                        var count = Count(call.Arguments[1]);
                        Take = Take.HasValue ? Math.Min(Take.Value, count) : count;
                        return;
                    }
                case "First":
                    EndSetTerminal(LinqTerminal.First, lambda);
                    return;
                case "FirstOrDefault":
                    EndSetTerminal(LinqTerminal.FirstOrDefault, lambda);
                    return;
                case "Single":
                    EndSetTerminal(LinqTerminal.Single, lambda);
                    return;
                case "SingleOrDefault":
                    EndSetTerminal(LinqTerminal.SingleOrDefault, lambda);
                    return;
                case "Count":
                    EndSetTerminal(LinqTerminal.Count, lambda);
                    return;
                case "LongCount":
                    EndSetTerminal(LinqTerminal.LongCount, lambda);
                    return;
                case "Any":
                    EndSetTerminal(LinqTerminal.Any, lambda);
                    return;
            }
            throw new NotSupportedException($"{call.Method.Name} after a set operation isn't supported in a repository query");
        }
        private void EndSetTerminal(LinqTerminal terminal, LambdaExpression? argument)
        {
            if (terminal is not (LinqTerminal.First or LinqTerminal.FirstOrDefault or LinqTerminal.Single or LinqTerminal.SingleOrDefault or LinqTerminal.Count or LinqTerminal.LongCount or LinqTerminal.Any))
                throw new NotSupportedException($"{terminal} after a set operation isn't supported in a repository query");
            Terminal = terminal;
            if (argument is not null)
                SetCondition(argument);
            //one row says what is first, two whether there is a single one
            if (terminal is LinqTerminal.First or LinqTerminal.FirstOrDefault or LinqTerminal.Single or LinqTerminal.SingleOrDefault)
            {
                var rows = terminal is LinqTerminal.Single or LinqTerminal.SingleOrDefault ? 2 : 1;
                Take = Take.HasValue ? Math.Min(Take.Value, rows) : rows;
            }
        }
        private void SetCondition(LambdaExpression condition)
        {
            if (Skip.HasValue || Take.HasValue)
                throw new NotSupportedException("A condition after Skip or Take isn't supported, filter before paging");
            if (Select is not null)
                SelectedHaving = SelectedHaving is null ? condition : AppendAnd(SelectedHaving, condition);
            else
                Having = Having is null ? condition : AppendAnd(Having, condition);
        }
        //the query before a set operation, of this query's model and elements
        private protected abstract LinqQuery SetPart(Expression expression);
        /// <summary>Combines the elements of the two queries of <see cref="Set"/>, compared by their values, and ends them as the query does.</summary>
        /// <param name="left">The elements of the first query.</param>
        /// <param name="right">The elements of the second query.</param>
        /// <param name="caseSensitive">Whether string comparisons in the condition and order after it are case sensitive.</param>
        /// <returns>The elements as a list of the result type, the count boxed as a long, or whether there are any.</returns>
        internal abstract object CombineSet(IEnumerable left, IEnumerable right, bool caseSensitive);

        //a query combined with another reads only its rows, which have no order unless it pages them
        internal void ReadAsSetPart()
        {
            if (Graph is not null)
                throw new NotSupportedException("A query combined with a set operation reads without a graph");
            if (!Skip.HasValue && !Take.HasValue)
                Order = null;
        }

        //the rows of a query another reads in a subquery: an order only matters to a page, and null is no value to look for
        internal void ReadAsSubquery(ModelMemberDetail? keyMember)
        {
            KeyMember = keyMember;
            if (!Skip.HasValue && !Take.HasValue)
                Order = null;
            if (Select is not null)
            {
                var value = Select.Body;
                if (Nullable.GetUnderlyingType(value.Type) is not null || !value.Type.IsValueType)
                {
                    var notNull = Expression.Lambda(ModelAnalyzer.GetModel(ModelType).LambdaDelegateType, Expression.NotEqual(value, Expression.Constant(null, value.Type)), Select.Parameters);
                    Where = Where is null ? notNull : AppendAnd(Where, notNull);
                }
            }
        }

        //the index is reached by skipping in the store, past the end of a Take there is no row to read
        private void EndAt(LinqTerminal terminal, int index)
        {
            Terminal = terminal;
            if (index < 0 || (Take.HasValue && index >= Take.Value))
            {
                Take = 0;
                Skip = null;
                return;
            }
            Skip = (Skip ?? 0) + index;
            Take = 1;
        }

        private void EndContains(Expression item)
        {
            if (GroupBy is not null)
                throw new NotSupportedException("Contains after GroupBy isn't supported, read the groups");
            if (Select is null || !IsScalarSelect)
                throw new NotSupportedException("Contains looks for one value, such as Select(x => x.ID).Contains(id)");
            if (Skip.HasValue || Take.HasValue)
                throw new NotSupportedException("Contains after Skip or Take isn't supported, filter before paging");
            var value = Select.Body;
            if (item.Type != value.Type)
                item = Expression.Convert(item, value.Type);
            var condition = Expression.Lambda(ModelAnalyzer.GetModel(ModelType).LambdaDelegateType, Expression.Equal(value, item), Select.Parameters);
            Select = null;
            End(LinqTerminal.Any, condition);
        }

        //the store reads the order the other way round; a page of the order is a different set of rows once reversed
        private void Reverse(string call)
        {
            if (Skip.HasValue || Take.HasValue)
                throw new NotSupportedException($"{call} after Skip or Take isn't supported, reverse the order before paging");
            if (GroupBy is not null && Select is not null)
                ResultOrder = Reversed(ResultOrder, call);
            else
                Order = Reversed(Order, call);
        }
        private static QueryOrder Reversed(QueryOrder? order, string call)
        {
            if (order is null)
                throw new NotSupportedException($"{call} needs an OrderBy to say which way round the elements are");
            var expressions = new OrderExpression[order.OrderExpressions.Length];
            for (var i = 0; i < expressions.Length; i++)
                expressions[i] = new ProjectionOrderExpression(order.OrderExpressions[i].Expression, !order.OrderExpressions[i].Descending);
            return new ProjectionOrder(expressions);
        }

        //the terminal call a condition came in, or null for a Where
        private void Condition(LambdaExpression condition, LinqTerminal? terminal)
        {
            if (GroupBy is not null && Select is not null)
            {
                Having = Having is null ? condition : AppendAnd(Having, condition);
                return;
            }
            if (Skip.HasValue || Take.HasValue)
                throw new NotSupportedException($"A condition in {terminal?.ToString() ?? "Where"} after Skip or Take isn't supported, filter before paging");
            condition = LinqSubquery.Rewrite(condition, out var found);
            HasSubquery |= found;
            if (Select is not null)
                condition = Expression.Lambda(ModelAnalyzer.GetModel(ModelType).LambdaDelegateType, LinqSelectRewriter.Rewrite(condition, Select), Select.Parameters);
            Where = Where is null ? condition : AppendAnd(Where, condition);
        }

        internal static int Count(Expression expression)
        {
            if (expression is ConstantExpression constant)
                return (int)constant.Value!;
            return Expression.Lambda<Func<int>>(expression).Compile()();
        }

        internal static LambdaExpression AppendAnd(LambdaExpression first, LambdaExpression second)
            => Expression.Lambda(first.Type, Expression.AndAlso(first.Body, LinqRebinder.RebindExpression(second.Body, second.Parameters[0], first.Parameters[0])), first.Parameters);
    }
}
