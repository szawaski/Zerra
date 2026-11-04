// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.ComponentModel;
using System.Linq.Expressions;
using Zerra.Linq;
using Zerra.Repository.Linq;
using Zerra.Repository.Reflection;

namespace Zerra.Repository.Linq
{
    /// <summary>
    /// A LINQ query from <see cref="IRepo.Query{TModel}"/>, read from its calls in order: the conditions, the order, the paging, the projection, the grouping, and the call that ends it.
    /// It runs through the model's provider, where a layer may add to or change the conditions, and the store converts it in one pass.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class LinqQuery
    {
        /// <summary>Gets the CLR type of the model being queried.</summary>
        public Type ModelType { get; }
        /// <summary>Gets the type of the elements, or of the value the query ends in.</summary>
        public Type ResultType { get; }
        /// <summary>Gets or sets the condition on the models, the Where calls as one. A layer swaps its own condition in for the call and puts the original back.</summary>
        public LambdaExpression? Where { get; set; }
        /// <summary>Gets the order of the models, the OrderBy and ThenBy calls as one, or <see langword="null"/>.</summary>
        public LinqOrder[]? Order { get; private set; }
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
        public LinqOrder[]? ResultOrder { get; private set; }
        /// <summary>Gets the condition on the elements a <see cref="Select"/> after a set operation builds, the Where calls after it as one.</summary>
        public LambdaExpression? SelectedHaving { get; private set; }
        /// <summary>Gets the order of the elements a <see cref="Select"/> after a set operation builds, after <see cref="ResultOrder"/>, or <see langword="null"/>.</summary>
        public LinqOrder[]? SelectedOrder { get; private set; }
        /// <summary>Gets whether an element the same as another counts once.</summary>
        public bool Distinct { get; private set; }
        /// <summary>Gets the call that ends the query.</summary>
        public LinqTerminal Terminal { get; private set; }
        /// <summary>Gets the value a Sum, Min, Max, or Average aggregates.</summary>
        public LambdaExpression? Value { get; private set; }
        /// <summary>Gets whether an Any came from an All, so its answer is turned round.</summary>
        public bool Negated { get; private set; }
        /// <summary>Gets whether no element is read as one element of the default, or of <see cref="DefaultValue"/>, from <c>DefaultIfEmpty</c>.</summary>
        public bool DefaultIfEmpty { get; private set; }
        /// <summary>Gets the value given for no element, by <c>DefaultIfEmpty</c> or <c>FirstOrDefault</c>, when <see cref="HasDefaultValue"/>.</summary>
        public object? DefaultValue { get; private set; }
        /// <summary>Gets whether <see cref="DefaultValue"/> was given.</summary>
        public bool HasDefaultValue { get; private set; }
        /// <summary>Gets what each element of a group is, from the element selector of <c>GroupBy</c>, or <see langword="null"/> for the model.</summary>
        public LambdaExpression? GroupElement { get; private set; }
        /// <summary>Gets the key in a <see cref="Select"/> that is GroupBy's result selector of the key and the group, or <see langword="null"/>.</summary>
        public ParameterExpression? GroupKey { get; private set; }
        /// <summary>Gets whether each group gives its key and how many it has, from <c>CountBy</c>.</summary>
        public bool CountsByKey { get; private set; }
        /// <summary>Gets whether the elements are the results of groups, which a condition or order after them is of.</summary>
        public bool GroupedResults => GroupBy is not null && (Select is not null || CountsByKey);

        /// <summary>Gets the members and relations of the models to read, or <see langword="null"/> for every member and no relations.</summary>
        public Graph? Graph { get; }
        //a graph given to the query reads its relations with the rows; a provider loading models reads their relations itself
        internal bool GraphRelations { get; }

        /// <summary>Gets whether the query gives models rather than projections or values.</summary>
        public bool ReturnsModels => Select is null && GroupBy is null && KeyMember is null && ReadsElements;
        /// <summary>Gets whether the query reads its elements, rather than a count, whether there is any, or an aggregate.</summary>
        public bool ReadsElements => Terminal is LinqTerminal.Sequence or LinqTerminal.First or LinqTerminal.FirstOrDefault or LinqTerminal.Single or LinqTerminal.SingleOrDefault or LinqTerminal.Last or LinqTerminal.LastOrDefault or LinqTerminal.ElementAt or LinqTerminal.ElementAtOrDefault or LinqTerminal.MinBy or LinqTerminal.MaxBy;
        /// <summary>Gets whether the condition looks in another query.</summary>
        public bool HasSubquery { get; private set; }
        /// <summary>Gets the two queries a set operation combines into the elements, whose condition is <see cref="Having"/> and order <see cref="ResultOrder"/>, built from by <see cref="Select"/> with its condition <see cref="SelectedHaving"/> and order <see cref="SelectedOrder"/>, then paged by <see cref="Skip"/> and <see cref="Take"/>; <see langword="null"/> for none.</summary>
        public LinqSet? Set { get; private set; }
        /// <summary>Gets the two queries a join pairs into the elements, followed as a set operation's elements are by <see cref="Having"/>, <see cref="ResultOrder"/>, <see cref="Select"/>, and the paging; <see langword="null"/> for none.</summary>
        public LinqJoin? Join { get; private set; }
        /// <summary>Gets whether the elements are combined from two queries, by a set operation or a join.</summary>
        public bool Combines => Set is not null || Join is not null;
        internal ModelMemberDetail? KeyMember { get; private set; }
        //the elements the whole query gives before the call that ends it
        private readonly Type? elementType;
        /// <summary>Gets what a query of the history of models reads, or <see langword="null"/> for a query of their current state.</summary>
        public LinqTemporal? Temporal { get; }
        /// <summary>Gets the members and relations of the models the query reads: its <see cref="Graph"/>, or for an event query the graph of the model of each event.</summary>
        public Graph? ModelGraph => Temporal is { Events: true } ? Temporal.Graph : Graph;
        /// <summary>Gets whether the elements are events, <see cref="EventModel{TModel}"/>, with the model as their <c>Model</c>.</summary>
        public bool ReadsEvents => Temporal is { Events: true };
        /// <summary>Gets whether the projection picks one value rather than building an object.</summary>
        public bool IsScalarSelect => Select is not null && Select.Body is not MemberInitExpression && Select.Body is not NewExpression;
        private protected LinqQuery(Type modelType, Type resultType, Expression expression, LinqTerminal? terminal, LambdaExpression? argument, Graph? graph, LinqElements? elements = null, LinqTemporal? temporal = null)
        {
            this.ModelType = modelType;
            this.ResultType = resultType;
            this.Temporal = temporal;
            elementType = expression.Type.IsGenericType && expression.Type.GetGenericArguments().Length == 1 ? expression.Type.GetGenericArguments()[0] : resultType;
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
                    Order = elements.Orders;
                Select = elements.Select ?? (elements.SelectBody is null ? null : ModelLambda(elements.SelectBody, elements.SelectParameter!));
            }
            else if (node is not ConstantExpression { Value: IQueryable })
            {
                throw new NotSupportedException("A repository query starts with IRepo.Query");
            }
            //a set operation or a join combines the query before it with another, so what follows is of the elements it gives
            for (var i = 0; i < calls.Count; i++)
            {
                if (calls[i].Method.DeclaringType != typeof(System.Linq.Queryable))
                    continue;
                var operation = SetOperation(calls[i].Method.Name);
                var joinKind = operation is null ? JoinOperation(calls[i]) : null;
                if (operation is null && joinKind is null)
                    continue;
                if (elements is not null || graph is not null || temporal is not null)
                    throw new NotSupportedException($"{calls[i].Method.Name} combines queries of current state without a graph or SelectMany");
                if (operation is not null)
                    Set = ReadSet(operation.Value, calls[i]);
                else
                    Join = ReadJoin(joinKind!.Value, calls[i]);
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
        private protected LinqQuery(Type modelType, Type resultType, LambdaExpression? where, LinqOrder[]? order, int? skip, int? take, Graph? graph, LinqTerminal terminal)
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
        /// <returns>The elements as a list of the result type, or the value boxed: a long count, a bool, or an aggregate.</returns>
        public abstract object? Execute(IEnumerable<object> models, bool caseSensitive);
        //models a provider read for this query alone, as an event store's states, are given as they are when the query reads them whole
        internal abstract object? ExecuteRead(IEnumerable<object> models, bool caseSensitive);

        private void Apply(MethodCallExpression call)
        {
            if (call.Method.DeclaringType != typeof(System.Linq.Queryable))
                throw new NotSupportedException($"{call.Method.Name} isn't supported in a repository query");
            if (ApplyOperator(call))
                return;
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
                        var grouped = GroupedResults;
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException("OrderBy after Skip or Take isn't supported, order before paging");
                        if (!grouped && Select is not null)
                            lambda = ModelLambda(LinqSelectRewriter.Rewrite(lambda, Select), Select.Parameters[0]);
                        var expression = new LinqOrder(lambda, call.Method.Name is "OrderByDescending" or "ThenByDescending");
                        var order = grouped ? ResultOrder : Order;
                        if (order is null || call.Method.Name is "OrderBy" or "OrderByDescending")
                        {
                            order = [expression];
                        }
                        else
                        {
                            var expressions = new LinqOrder[order.Length + 1];
                            order.CopyTo(expressions, 0);
                            expressions[^1] = expression;
                            order = expressions;
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
                    if (CountsByKey)
                        throw new NotSupportedException("CountBy gives each key and its count, read them as they are or group with GroupBy and Select");
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

        //the calls whose arguments aren't the element's lambda, and those that only change how the elements are typed or ordered
        private bool ApplyOperator(MethodCallExpression call)
        {
            var elementType = SourceElement(call);
            switch (call.Method.Name)
            {
                case "Cast":
                    {
                        //the elements are what they are, so only a cast LINQ makes without changing them
                        var type = call.Method.GetGenericArguments()[0];
                        if (!type.IsAssignableFrom(elementType) && Nullable.GetUnderlyingType(type) != elementType)
                            throw new NotSupportedException($"Cast to {type.Name} changes the elements, which are {elementType.Name}; convert them in a Select");
                        return true;
                    }
                case "OfType":
                    {
                        var type = call.Method.GetGenericArguments()[0];
                        if (!type.IsAssignableFrom(elementType) && Nullable.GetUnderlyingType(elementType) != type)
                            throw new NotSupportedException($"OfType {type.Name} picks elements of a type they can't be, which are {elementType.Name}");
                        //an element of the type is any that isn't null
                        if (GroupBy is not null || (elementType.IsValueType && Nullable.GetUnderlyingType(elementType) is null))
                            return true;
                        if (Select is null || !IsScalarSelect)
                            return true;
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException("OfType after Skip or Take isn't supported, filter before paging");
                        var notNull = Expression.Lambda(ModelAnalyzer.GetModel(ModelType).LambdaDelegateType, Expression.NotEqual(Select.Body, Expression.Constant(null, Select.Body.Type)), Select.Parameters);
                        Where = Where is null ? notNull : AppendAnd(Where, notNull);
                        return true;
                    }
                case "Order":
                case "OrderDescending":
                    {
                        //the elements are the values ordered, what the Select builds from each model
                        if (GroupBy is not null || Select is null || !IsScalarSelect)
                            throw new NotSupportedException($"{call.Method.Name} orders values, such as Select(x => x.Price).{call.Method.Name}(); order models with OrderBy");
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException($"{call.Method.Name} after Skip or Take isn't supported, order before paging");
                        Order = [new LinqOrder(Select, call.Method.Name == "OrderDescending")];
                        return true;
                    }
                case "FirstOrDefault":
                case "SingleOrDefault":
                case "LastOrDefault":
                    {
                        //the overloads with the value given when there is no element
                        var parameters = call.Method.GetParameters();
                        if (parameters[^1].ParameterType != elementType)
                            return false;
                        DefaultValue = Evaluate(call.Arguments[^1]);
                        HasDefaultValue = true;
                        End(call.Method.Name switch { "FirstOrDefault" => LinqTerminal.FirstOrDefault, "SingleOrDefault" => LinqTerminal.SingleOrDefault, _ => LinqTerminal.LastOrDefault }, call.Arguments.Count == 3 ? Quoted(call.Arguments[1]) : null);
                        return true;
                    }
                case "MinBy":
                case "MaxBy":
                    {
                        if (call.Arguments.Count != 2)
                            throw new NotSupportedException($"{call.Method.Name} with a comparer isn't supported in a repository query, keys compare as the store compares them");
                        EndBy(call.Method.Name == "MinBy" ? LinqTerminal.MinBy : LinqTerminal.MaxBy, Quoted(call.Arguments[1]));
                        return true;
                    }
                case "GroupBy":
                    {
                        if (call.Arguments.Count == 2)
                            return false;
                        if (Select is not null || GroupBy is not null || Order is not null || Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException("GroupBy comes after the conditions and before a Select of each group");
                        //an element selector, a result selector of the key and the group, or both
                        var key = Quoted(call.Arguments[1]);
                        var selectors = new List<LambdaExpression>();
                        for (var i = 2; i < call.Arguments.Count; i++)
                        {
                            if (call.Arguments[i].NodeType != ExpressionType.Quote)
                                throw new NotSupportedException("GroupBy with a comparer isn't supported in a repository query, keys compare as the store compares them");
                            selectors.Add(Quoted(call.Arguments[i]));
                        }
                        GroupBy = key;
                        foreach (var selector in selectors)
                        {
                            if (selector.Parameters.Count == 1)
                            {
                                GroupElement = selector;
                            }
                            else
                            {
                                Select = selector;
                                GroupKey = selector.Parameters[0];
                            }
                        }
                        return true;
                    }
                case "CountBy":
                    {
                        if (call.Arguments.Count > 2 && call.Arguments[2] is not ConstantExpression { Value: null })
                            throw new NotSupportedException("CountBy with a comparer isn't supported in a repository query, keys compare as the store compares them");
                        if (Select is not null || GroupBy is not null || Order is not null || Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException("CountBy comes after the conditions");
                        GroupBy = Quoted(call.Arguments[1]);
                        CountsByKey = true;
                        return true;
                    }
                case "DefaultIfEmpty":
                    {
                        //no element is one of the default, or of the value given
                        if (call.Arguments.Count == 2)
                        {
                            DefaultValue = Evaluate(call.Arguments[1]);
                            HasDefaultValue = true;
                        }
                        DefaultIfEmpty = true;
                        return true;
                    }
                case "Take":
                    if (call.Arguments[1].Type != typeof(Range))
                        return false;
                    TakeRange((Range)Evaluate(call.Arguments[1])!);
                    return true;
                case "Chunk":
                    throw new NotSupportedException("Chunk comes last, read the chunks by enumerating the query");
                case "Aggregate":
                case "AggregateBy":
                    throw new NotSupportedException($"{call.Method.Name} runs a function no store can run; read the elements and aggregate them");
                default:
                    if (DefaultIfEmpty && call.Method.Name is "Where" or "Select" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Skip" or "Take" or "GroupBy" or "Distinct" or "Reverse")
                        throw new NotSupportedException("DefaultIfEmpty comes last, before the call that ends the query");
                    return false;
            }
        }
        //the elements a call is given, as the queryable before it is typed
        private static Type SourceElement(MethodCallExpression call)
        {
            var source = call.Arguments[0].Type;
            if (source.IsGenericType && source.GetGenericArguments().Length == 1)
                return source.GetGenericArguments()[0];
            return call.Method.IsGenericMethod ? call.Method.GetGenericArguments()[0] : typeof(object);
        }
        private static object? Evaluate(Expression expression)
        {
            if (expression is ConstantExpression constant)
                return constant.Value;
            return Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile()();
        }
        //the element with the smallest or largest key, the first of the order by it, which the order before breaks ties in
        private void EndBy(LinqTerminal terminal, LambdaExpression key)
        {
            if (Skip.HasValue || Take.HasValue)
                throw new NotSupportedException($"{terminal} after Skip or Take isn't supported, page after it");
            var grouped = GroupedResults;
            if (!grouped && Select is not null)
                key = ModelLambda(LinqSelectRewriter.Rewrite(key, Select), Select.Parameters[0]);
            var by = new LinqOrder(key, terminal == LinqTerminal.MaxBy);
            var order = grouped ? ResultOrder : Order;
            if (order is null)
            {
                order = [by];
            }
            else
            {
                var expressions = new LinqOrder[order.Length + 1];
                expressions[0] = by;
                order.CopyTo(expressions, 1);
                order = expressions;
            }
            if (grouped)
                ResultOrder = order;
            else
                Order = order;
            Terminal = terminal;
            Take = 1;
        }

        private void End(LinqTerminal terminal, LambdaExpression? argument)
        {
            //the default element is the one element of none, which a condition or value of it can't be read from the store for
            if (DefaultIfEmpty && (argument is not null || terminal is LinqTerminal.ElementAt or LinqTerminal.ElementAtOrDefault))
                throw new NotSupportedException($"{terminal} after DefaultIfEmpty takes no condition or value, put them before DefaultIfEmpty");
            if (DefaultIfEmpty && terminal == LinqTerminal.Sum && HasDefaultValue && !Equals(DefaultValue, 0) && !Equals(DefaultValue, 0L) && !Equals(DefaultValue, 0m) && !Equals(DefaultValue, 0d) && !Equals(DefaultValue, 0f))
                throw new NotSupportedException("Sum after DefaultIfEmpty of a value other than zero isn't supported");
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
                        throw new NotSupportedException($"{terminal} after GroupBy isn't supported, aggregate it in the Select of each group");
                    if (argument is null)
                    {
                        if (!IsScalarSelect || Select!.ReturnType == typeof(object))
                            throw new NotSupportedException($"{terminal} takes the value to aggregate");
                        Value = Select;
                        Select = null;
                    }
                    else
                    {
                        if (Select is not null)
                            throw new NotSupportedException($"{terminal} after Select isn't supported, aggregate the value of the models");
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

        //a GroupJoin flattened by SelectMany is a join of the pairs, with no element when SelectMany reads DefaultIfEmpty of the group
        private static LinqJoinKind? JoinOperation(MethodCallExpression call)
        {
            switch (call.Method.Name)
            {
                case "Join":
                    return LinqJoinKind.Inner;
                case "LeftJoin":
                    return LinqJoinKind.Left;
                case "RightJoin":
                    return LinqJoinKind.Right;
                case "GroupJoin":
                    return LinqJoinKind.Group;
                case "SelectMany":
                    if (call.Arguments.Count != 3 || call.Arguments[0] is not MethodCallExpression { Method.Name: "GroupJoin" } groupJoin || groupJoin.Method.DeclaringType != typeof(System.Linq.Queryable))
                        return null;
                    var collection = Quoted(call.Arguments[1]);
                    return collection.Body is MethodCallExpression { Method.Name: "DefaultIfEmpty", Arguments.Count: 1 } empty && empty.Method.DeclaringType == typeof(Enumerable) ? LinqJoinKind.Left : LinqJoinKind.Inner;
                case "Zip":
                    return LinqJoinKind.Zip;
                case "SequenceEqual":
                    return LinqJoinKind.SequenceEqual;
                case "Index":
                    return LinqJoinKind.Index;
                case "Select":
                case "Where":
                    //the overloads taking the place of each element
                    return call.Arguments.Count == 2 && Quoted(call.Arguments[1]).Parameters.Count == 2 ? LinqJoinKind.Index : null;
                case "TakeWhile":
                case "SkipWhile":
                    return LinqJoinKind.While;
                case "TakeLast":
                case "SkipLast":
                    return LinqJoinKind.Last;
                case "Take":
                    return call.Arguments[1].Type == typeof(Range) && Evaluate(call.Arguments[1]) is Range range && (range.Start.IsFromEnd || range.End.IsFromEnd) ? LinqJoinKind.Last : null;
                case "ElementAt":
                case "ElementAtOrDefault":
                    return call.Arguments[1].Type == typeof(Index) && Evaluate(call.Arguments[1]) is Index index && index.IsFromEnd ? LinqJoinKind.Last : null;
                case "DistinctBy":
                case "IntersectBy":
                case "ExceptBy":
                    return LinqJoinKind.DistinctBy;
                case "Append":
                case "Prepend":
                    return LinqJoinKind.Append;
                case "UnionBy":
                    return LinqJoinKind.UnionBy;
                default:
                    return null;
            }
        }
        private static void NoComparer(MethodCallExpression call, int arguments)
        {
            if (call.Arguments.Count > arguments && call.Arguments[arguments] is not ConstantExpression { Value: null })
                throw new NotSupportedException($"{call.Method.Name} with a comparer isn't supported in a repository query, elements compare by their values");
        }
        //an operation by position on one or two queries, each with its rows numbered in its order
        private LinqJoin ReadPosition(LinqJoinKind kind, MethodCallExpression call)
        {
            var name = call.Method.Name;
            var elementType = SourceElement(call);
            var element = Expression.Parameter(elementType, "x");
            var place = Expression.Parameter(typeof(int), "i");
            RepoQueryProvider? provider = null;
            LinqQuery Second(Expression argument)
            {
                object? source;
                try
                {
                    source = Expression.Lambda<Func<object?>>(Expression.Convert(argument, typeof(object))).Compile()();
                }
                catch (InvalidOperationException ex)
                {
                    throw new NotSupportedException($"The query {name} reads with is built from itself alone", ex);
                }
                if (source is not IQueryable { Provider: RepoQueryProvider second } queryable)
                    throw new NotSupportedException($"{name} reads queries from IRepo.Query");
                provider = second;
                return second.ReadNumbered(queryable.Expression, name);
            }
            switch (kind)
            {
                case LinqJoinKind.Zip:
                    {
                        if (call.Arguments.Count > 3 || (call.Arguments.Count == 3 && call.Arguments[2].NodeType != ExpressionType.Quote))
                            throw new NotSupportedException("Zip of three queries isn't supported in a repository query, zip two and then the third");
                        var left = NumberedPart(call.Arguments[0], name);
                        var right = Second(call.Arguments[1]);
                        if (call.Arguments.Count == 3)
                        {
                            var result = Quoted(call.Arguments[2]);
                            return new LinqJoin(provider!.Repo, kind, left, right, null, null, result.Body, result.Parameters[0], result.Parameters[1]);
                        }
                        return new LinqJoin(provider!.Repo, kind, left, right, null, null, null, element, Expression.Parameter(SourceElement(call, 1), "y")) { TupleType = ElementTypeOf(call.Type) };
                    }
                case LinqJoinKind.SequenceEqual:
                    {
                        NoComparer(call, 2);
                        var left = NumberedPart(call.Arguments[0], name);
                        var right = Second(call.Arguments[1]);
                        //the same elements are none that differ
                        Terminal = LinqTerminal.Any;
                        Negated = true;
                        return new LinqJoin(provider!.Repo, kind, left, right, null, null, null, element, Expression.Parameter(elementType, "y"));
                    }
                case LinqJoinKind.Index:
                    {
                        var left = NumberedPart(call.Arguments[0], name);
                        if (name == "Index")
                            return new LinqJoin(RepoOf(call.Arguments[0]), kind, left, null, null, null, null, element, place) { TupleType = ElementTypeOf(call.Type) };
                        var lambda = Quoted(call.Arguments[1]);
                        if (name == "Select")
                            return new LinqJoin(RepoOf(call.Arguments[0]), kind, left, null, null, null, lambda.Body, lambda.Parameters[0], lambda.Parameters[1]);
                        return new LinqJoin(RepoOf(call.Arguments[0]), kind, left, null, null, null, lambda.Parameters[0], lambda.Parameters[0], lambda.Parameters[1]) { Condition = lambda.Body };
                    }
                case LinqJoinKind.While:
                    {
                        var left = NumberedPart(call.Arguments[0], name);
                        var lambda = Quoted(call.Arguments[1]);
                        var placed = lambda.Parameters.Count == 2 ? lambda.Parameters[1] : place;
                        return new LinqJoin(RepoOf(call.Arguments[0]), kind, left, null, null, null, lambda.Parameters[0], lambda.Parameters[0], placed) { Condition = lambda.Body, Skips = name == "SkipWhile" };
                    }
                case LinqJoinKind.Last:
                    {
                        var left = NumberedPart(call.Arguments[0], name);
                        var count = 0;
                        var skips = false;
                        var at = false;
                        if (name is "TakeLast" or "SkipLast")
                        {
                            count = Count(call.Arguments[1]);
                            skips = name == "SkipLast";
                        }
                        else if (name == "Take")
                        {
                            //a range from the end is the last elements, one to the end from the start all but them
                            var range = (Range)Evaluate(call.Arguments[1])!;
                            if (range.Start.IsFromEnd && range.End.Equals(Index.End))
                            {
                                count = range.Start.Value;
                            }
                            else if (range.Start.Equals(Index.Start) && range.End.IsFromEnd)
                            {
                                count = range.End.Value;
                                skips = true;
                            }
                            else
                            {
                                throw new NotSupportedException("Take of a range is supported from the start, from the end to the end, or from the start to the end less some");
                            }
                        }
                        else
                        {
                            //the element counted from the end, which there may not be
                            count = ((Index)Evaluate(call.Arguments[1])!).Value;
                            at = true;
                            Terminal = name == "ElementAt" ? LinqTerminal.ElementAt : LinqTerminal.ElementAtOrDefault;
                            Take = 1;
                        }
                        if (count < 0)
                            throw new ArgumentOutOfRangeException("count");
                        return new LinqJoin(RepoOf(call.Arguments[0]), kind, left, null, null, null, element, element, place) { Count = count, Skips = skips, At = at };
                    }
                case LinqJoinKind.DistinctBy:
                    {
                        var key = Quoted(call.Arguments[name == "DistinctBy" ? 1 : 2]);
                        NoComparer(call, name == "DistinctBy" ? 2 : 3);
                        if (name == "DistinctBy")
                            return new LinqJoin(RepoOf(call.Arguments[0]), kind, NumberedPart(call.Arguments[0], name), null, key, null, key.Parameters[0], key.Parameters[0], place);
                        //the elements whose key the other query has, or doesn't, each key once
                        object? source;
                        try
                        {
                            source = Expression.Lambda<Func<object?>>(Expression.Convert(call.Arguments[1], typeof(object))).Compile()();
                        }
                        catch (InvalidOperationException ex)
                        {
                            throw new NotSupportedException($"The keys {name} looks in are built from the query alone", ex);
                        }
                        if (source is not IQueryable { Provider: RepoQueryProvider keys } queryable)
                            throw new NotSupportedException($"{name} looks in keys from IRepo.Query, such as Select(x => x.ID); for keys in memory use Where(x => keys.Contains(..)).DistinctBy(..)");
                        var subquery = keys.ReadSubquery(queryable.Expression);
                        Expression contains = Expression.Call(Expression.Constant(subquery), LinqSubquery.ContainsMethod, Expression.Convert(key.Body, typeof(object)));
                        if (name == "ExceptBy")
                            contains = Expression.Not(contains);
                        var part = NumberedPart(call.Arguments[0], name);
                        part.AddCondition(key.Parameters[0], contains, name);
                        return new LinqJoin(keys.Repo, kind, part, null, key, null, key.Parameters[0], key.Parameters[0], place);
                    }
                case LinqJoinKind.Append:
                    return new LinqJoin(RepoOf(call.Arguments[0]), kind, NumberedPart(call.Arguments[0], name), null, null, null, element, element, place) { Value = Evaluate(call.Arguments[1]), Prepends = name == "Prepend" };
                case LinqJoinKind.UnionBy:
                    {
                        NoComparer(call, 3);
                        var key = Quoted(call.Arguments[2]);
                        var left = NumberedPart(call.Arguments[0], name);
                        var right = Second(call.Arguments[1]);
                        return new LinqJoin(provider!.Repo, kind, left, right, key, null, key.Parameters[0], key.Parameters[0], place);
                    }
                default:
                    throw new NotSupportedException($"{name} isn't supported in a repository query");
            }
        }
        //the repository of the query an operation reads, found at its root
        private static IRepoInternal RepoOf(Expression expression)
        {
            while (expression is MethodCallExpression call && call.Arguments.Count > 0)
                expression = call.Arguments[0];
            if (expression is ConstantExpression { Value: IQueryable { Provider: RepoQueryProvider provider } })
                return provider.Repo;
            throw new NotSupportedException("A repository query starts with IRepo.Query");
        }
        private static Type ElementTypeOf(Type queryable) => queryable.IsGenericType && queryable.GetGenericArguments().Length == 1 ? queryable.GetGenericArguments()[0] : typeof(object);
        private static Type SourceElement(MethodCallExpression call, int argument)
        {
            var source = call.Arguments[argument].Type;
            return source.IsGenericType && source.GetGenericArguments().Length == 1 ? source.GetGenericArguments()[0] : typeof(object);
        }
        private LinqJoin ReadJoin(LinqJoinKind kind, MethodCallExpression call)
        {
            if (kind is not (LinqJoinKind.Inner or LinqJoinKind.Left or LinqJoinKind.Right or LinqJoinKind.Group))
                return ReadPosition(kind, call);
            Expression result;
            ParameterExpression leftElement;
            ParameterExpression rightElement;
            if (call.Method.Name == "SelectMany")
            {
                var groupJoin = (MethodCallExpression)call.Arguments[0];
                if (groupJoin.Arguments.Count != 5)
                    throw new NotSupportedException("GroupJoin with a comparer isn't supported in a repository query, keys compare by their values");
                result = FlattenGroupJoin(Quoted(groupJoin.Arguments[4]), Quoted(call.Arguments[1]), Quoted(call.Arguments[2]), out leftElement, out rightElement);
                call = groupJoin;
            }
            else
            {
                if (call.Arguments.Count != 5)
                    throw new NotSupportedException($"{call.Method.Name} with a comparer isn't supported in a repository query, keys compare by their values");
                var built = Quoted(call.Arguments[4]);
                result = built.Body;
                leftElement = built.Parameters[0];
                rightElement = built.Parameters[1];
            }
            var left = SetPart(call.Arguments[0]);
            object? source;
            try
            {
                source = Expression.Lambda<Func<object?>>(Expression.Convert(call.Arguments[1], typeof(object))).Compile()();
            }
            catch (InvalidOperationException ex)
            {
                throw new NotSupportedException($"The query {call.Method.Name} pairs with is built from itself alone", ex);
            }
            if (source is not IQueryable { Provider: RepoQueryProvider provider } queryable)
                throw new NotSupportedException($"{call.Method.Name} pairs queries from IRepo.Query");
            var leftKey = Quoted(call.Arguments[2]);
            var rightKey = Quoted(call.Arguments[3]);
            if (leftKey.Body is NewExpression leftParts && rightKey.Body is NewExpression rightParts && leftParts.Arguments.Count != rightParts.Arguments.Count)
                throw new NotSupportedException("The keys of a join have the same parts");
            return provider.ReadJoin(kind, left, queryable.Expression, leftKey, rightKey, result, leftElement, rightElement);
        }
        //GroupJoin(.., (o, g) => new { o, g }).SelectMany(t => t.g.DefaultIfEmpty(), (t, i) => r) is the pair (o, i) building r, each member of t read as GroupJoin built it
        private static Expression FlattenGroupJoin(LambdaExpression grouped, LambdaExpression collection, LambdaExpression result, out ParameterExpression outer, out ParameterExpression inner)
        {
            outer = grouped.Parameters[0];
            var group = grouped.Parameters[1];
            var source = collection.Body;
            if (source is MethodCallExpression { Method.Name: "DefaultIfEmpty" } empty)
                source = empty.Arguments[0];
            var readsGroup = source == collection.Parameters[0] && grouped.Body == group;
            if (!readsGroup && source is MemberExpression { Expression: ParameterExpression owner } groupMember && owner == collection.Parameters[0] && grouped.Body is NewExpression { Members: not null } built)
            {
                for (var i = 0; i < built.Members.Count; i++)
                {
                    if (built.Members[i].Name == groupMember.Member.Name)
                        readsGroup = built.Arguments[i] == group;
                }
            }
            if (!readsGroup)
                throw new NotSupportedException("SelectMany after GroupJoin reads the group, or its DefaultIfEmpty, as it is");
            inner = result.Parameters[1];
            var pair = grouped.Body == group ? (Expression)Expression.Default(group.Type) : new ParameterReplacer(group, Expression.Default(group.Type)).Visit(grouped.Body)!;
            var body = new ParameterReplacer(result.Parameters[0], pair).Visit(result.Body)!;
            return NewMemberSimplifier.Instance.Visit(body)!;
        }
        private static LambdaExpression Quoted(Expression argument)
            => (LambdaExpression)(argument.NodeType == ExpressionType.Quote ? ((UnaryExpression)argument).Operand : argument);
        private sealed class ParameterReplacer(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => node == parameter ? replacement : node;
        }
        //a member of an anonymous object built in the expression is the value it was built from
        private sealed class NewMemberSimplifier : ExpressionVisitor
        {
            public static readonly NewMemberSimplifier Instance = new();
            protected override Expression VisitMember(MemberExpression node)
            {
                var owner = Visit(node.Expression);
                if (owner is NewExpression { Members: not null } built)
                {
                    for (var i = 0; i < built.Members.Count; i++)
                    {
                        if (built.Members[i].Name == node.Member.Name)
                            return built.Arguments[i];
                    }
                }
                return node.Update(owner);
            }
        }
        //what follows a set is of its elements, as a grouped query's having and order are of its results
        private void EndSet(MethodCallExpression call)
        {
            if (call.Method.DeclaringType == typeof(System.Linq.Queryable) && EndSetOperator(call))
                return;
            if (call.Method.DeclaringType != typeof(System.Linq.Queryable) || call.Arguments.Count > 2)
                throw new NotSupportedException($"{call.Method.Name} after a set operation or join isn't supported in a repository query");
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
                    if (Distinct)
                        throw new NotSupportedException("Distinct before a Select of combined elements isn't supported, take the different elements the Select builds");
                    Select = lambda ?? throw new NotSupportedException("Select takes what to build from each element");
                    return;
                case "Distinct":
                    if (call.Arguments.Count > 1)
                        throw new NotSupportedException("Distinct with a comparer isn't supported in a repository query");
                    if (Skip.HasValue || Take.HasValue)
                        throw new NotSupportedException("Distinct after Skip or Take isn't supported, take the different elements before them");
                    if (Select is not null && ResultOrder is not null)
                        throw new NotSupportedException("Distinct of what a Select builds is ordered by what it builds, so order after the Select");
                    Distinct = true;
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
                        var expression = new LinqOrder(lambda, call.Method.Name is "OrderByDescending" or "ThenByDescending");
                        var restart = call.Method.Name is "OrderBy" or "OrderByDescending";
                        if (Distinct && Select is not null && !restart && ResultOrder is not null)
                            throw new NotSupportedException("Distinct of what a Select builds is ordered by what it builds, so order after the Select");
                        if (Select is not null)
                        {
                            if (restart || (ResultOrder is null && SelectedOrder is null))
                            {
                                ResultOrder = null;
                                SelectedOrder = [expression];
                            }
                            else
                            {
                                var selected = SelectedOrder ?? [];
                                var appended = new LinqOrder[selected.Length + 1];
                                selected.CopyTo(appended, 0);
                                appended[^1] = expression;
                                SelectedOrder = appended;
                            }
                            return;
                        }
                        if (ResultOrder is null || restart)
                        {
                            ResultOrder = [expression];
                        }
                        else
                        {
                            var expressions = new LinqOrder[ResultOrder.Length + 1];
                            ResultOrder.CopyTo(expressions, 0);
                            expressions[^1] = expression;
                            ResultOrder = expressions;
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
                case "All":
                    //whether every element meets a condition is whether none fails it
                    Negated = true;
                    EndSetTerminal(LinqTerminal.Any, Expression.Lambda(lambda!.Type, Expression.Not(lambda.Body), lambda.Parameters));
                    return;
                case "Last":
                    EndSetTerminal(LinqTerminal.Last, lambda);
                    return;
                case "LastOrDefault":
                    EndSetTerminal(LinqTerminal.LastOrDefault, lambda);
                    return;
                case "ElementAt":
                    EndSetAt(LinqTerminal.ElementAt, Count(call.Arguments[1]));
                    return;
                case "ElementAtOrDefault":
                    EndSetAt(LinqTerminal.ElementAtOrDefault, Count(call.Arguments[1]));
                    return;
                case "Sum":
                    EndSetTerminal(LinqTerminal.Sum, lambda);
                    return;
                case "Min":
                    EndSetTerminal(LinqTerminal.Min, lambda);
                    return;
                case "Max":
                    EndSetTerminal(LinqTerminal.Max, lambda);
                    return;
                case "Average":
                    EndSetTerminal(LinqTerminal.Average, lambda);
                    return;
            }
            throw new NotSupportedException($"{call.Method.Name} after a set operation or join isn't supported in a repository query");
        }
        //the calls after a set or join whose arguments aren't the element's lambda, and those that only change how the elements are typed or ordered
        private bool EndSetOperator(MethodCallExpression call)
        {
            var elementType = SourceElement(call);
            switch (call.Method.Name)
            {
                case "Cast":
                    {
                        var type = call.Method.GetGenericArguments()[0];
                        if (!type.IsAssignableFrom(elementType) && Nullable.GetUnderlyingType(type) != elementType)
                            throw new NotSupportedException($"Cast to {type.Name} changes the elements, which are {elementType.Name}; convert them in a Select");
                        return true;
                    }
                case "OfType":
                    {
                        var type = call.Method.GetGenericArguments()[0];
                        if (!type.IsAssignableFrom(elementType) && Nullable.GetUnderlyingType(elementType) != type)
                            throw new NotSupportedException($"OfType {type.Name} picks elements of a type they can't be, which are {elementType.Name}");
                        //an element of the type is a value that isn't null, an object built is never null
                        if (Nullable.GetUnderlyingType(elementType) is not null || elementType == typeof(string))
                            SetCondition(ElementLambda(elementType, element => Expression.NotEqual(element, Expression.Constant(null, elementType))));
                        return true;
                    }
                case "Order":
                case "OrderDescending":
                    {
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException($"{call.Method.Name} after Skip or Take isn't supported, order before paging");
                        var order = new LinqOrder(ElementLambda(elementType, element => element), call.Method.Name == "OrderDescending");
                        if (Select is not null)
                        {
                            ResultOrder = null;
                            SelectedOrder = [order];
                        }
                        else
                        {
                            ResultOrder = [order];
                        }
                        return true;
                    }
                case "MinBy":
                case "MaxBy":
                    {
                        if (call.Arguments.Count != 2)
                            throw new NotSupportedException($"{call.Method.Name} with a comparer isn't supported in a repository query, keys compare as the store compares them");
                        if (Skip.HasValue || Take.HasValue)
                            throw new NotSupportedException($"{call.Method.Name} after Skip or Take isn't supported, page after it");
                        var by = new LinqOrder(Quoted(call.Arguments[1]), call.Method.Name == "MaxBy");
                        if (Select is not null)
                            SelectedOrder = [by, .. SelectedOrder ?? []];
                        else
                            ResultOrder = [by, .. ResultOrder ?? []];
                        Terminal = call.Method.Name == "MinBy" ? LinqTerminal.MinBy : LinqTerminal.MaxBy;
                        Take = 1;
                        return true;
                    }
                case "FirstOrDefault":
                case "SingleOrDefault":
                case "LastOrDefault":
                    {
                        var parameters = call.Method.GetParameters();
                        if (parameters[^1].ParameterType != elementType)
                            return false;
                        DefaultValue = Evaluate(call.Arguments[^1]);
                        HasDefaultValue = true;
                        EndSetTerminal(call.Method.Name switch { "FirstOrDefault" => LinqTerminal.FirstOrDefault, "SingleOrDefault" => LinqTerminal.SingleOrDefault, _ => LinqTerminal.LastOrDefault }, call.Arguments.Count == 3 ? Quoted(call.Arguments[1]) : null);
                        return true;
                    }
                case "DefaultIfEmpty":
                    if (call.Arguments.Count == 2)
                    {
                        DefaultValue = Evaluate(call.Arguments[1]);
                        HasDefaultValue = true;
                    }
                    DefaultIfEmpty = true;
                    return true;
                case "Take":
                    if (call.Arguments[1].Type != typeof(Range))
                        return false;
                    TakeRange((Range)Evaluate(call.Arguments[1])!);
                    return true;
                case "Aggregate":
                case "AggregateBy":
                    throw new NotSupportedException($"{call.Method.Name} runs a function no store can run; read the elements and aggregate them");
                default:
                    if (DefaultIfEmpty && call.Method.Name is "Where" or "Select" or "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Skip" or "Take" or "Distinct")
                        throw new NotSupportedException("DefaultIfEmpty comes last, before the call that ends the query");
                    return false;
            }
        }
        //a lambda over an element of any type, taken as an object so no delegate of the element's type is made
        private static LambdaExpression ElementLambda(Type elementType, Func<Expression, Expression> body)
        {
            var element = Expression.Parameter(typeof(object), "element");
            var value = body(Expression.Convert(element, elementType));
            if (value.Type == typeof(bool))
                return Expression.Lambda<Func<object?, bool>>(value, element);
            return Expression.Lambda<Func<object?, object?>>(Expression.Convert(value, typeof(object)), element);
        }
        private void EndSetTerminal(LinqTerminal terminal, LambdaExpression? argument)
        {
            if (DefaultIfEmpty && argument is not null)
                throw new NotSupportedException($"{terminal} after DefaultIfEmpty takes no condition or value, put them before DefaultIfEmpty");
            if (terminal is LinqTerminal.Sum or LinqTerminal.Min or LinqTerminal.Max or LinqTerminal.Average)
            {
                Terminal = terminal;
                if (argument is null && Select is null)
                {
                    //the value is the element
                    Value = ElementLambda(elementType!, element => element);
                }
                else if (argument is null)
                {
                    //the value is what the Select builds, so nothing else may read what it builds
                    if (!IsScalarSelect || Select!.ReturnType == typeof(object))
                        throw new NotSupportedException($"{terminal} takes the value to aggregate");
                    if (SelectedHaving is not null || SelectedOrder is not null || Distinct)
                        throw new NotSupportedException($"{terminal} of what a Select builds is aggregated before a condition, order, or Distinct of it, so aggregate the value of the elements");
                    Value = Select;
                    Select = null;
                }
                else
                {
                    if (Select is not null)
                        throw new NotSupportedException($"{terminal} after Select isn't supported, aggregate the value of the elements");
                    Value = argument;
                }
                return;
            }
            if (terminal is LinqTerminal.Last or LinqTerminal.LastOrDefault)
            {
                Terminal = terminal;
                if (argument is not null)
                    SetCondition(argument);
                //the last is the first of the order the other way round
                if (Skip.HasValue || Take.HasValue)
                    throw new NotSupportedException($"{terminal} after Skip or Take isn't supported, reverse the order before paging");
                if (ResultOrder is null && SelectedOrder is null)
                    throw new NotSupportedException($"{terminal} needs an OrderBy to say which way round the elements are");
                if (ResultOrder is not null)
                    ResultOrder = Reversed(ResultOrder, terminal.ToString());
                if (SelectedOrder is not null)
                    SelectedOrder = Reversed(SelectedOrder, terminal.ToString());
                Take = 1;
                return;
            }
            if (terminal is not (LinqTerminal.First or LinqTerminal.FirstOrDefault or LinqTerminal.Single or LinqTerminal.SingleOrDefault or LinqTerminal.Count or LinqTerminal.LongCount or LinqTerminal.Any))
                throw new NotSupportedException($"{terminal} after a set operation or join isn't supported in a repository query");
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
        //a range from the start is a skip then a take, those from the end read the places of the elements
        private void TakeRange(Range range)
        {
            if (range.Start.IsFromEnd || range.End.IsFromEnd)
                throw new NotSupportedException("Take of a range from the end is read by its place");
            if (Take.HasValue && range.Start.Value > 0)
                throw new NotSupportedException("Skip after Take isn't supported, skip before taking");
            Skip = (Skip ?? 0) + range.Start.Value;
            var count = Math.Max(0, range.End.Value - range.Start.Value);
            Take = Take.HasValue ? Math.Min(Take.Value, count) : count;
        }
        //the index is reached by skipping, past the end of a Take there is no element to read
        private void EndSetAt(LinqTerminal terminal, int index)
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
        //the query before an operation by position, its rows numbered in its order
        private protected abstract LinqQuery NumberedPart(Expression expression, string operation);
        //a condition on the elements from IntersectBy or ExceptBy, read onto the model through the Select
        internal void AddCondition(ParameterExpression element, Expression condition, string operation)
        {
            if (Skip.HasValue || Take.HasValue)
                throw new NotSupportedException($"{operation} after Skip or Take isn't supported, filter before paging");
            HasSubquery = true;
            ParameterExpression model;
            if (Select is not null)
            {
                condition = new LinqSelectRewriter(element, Select.Body).Visit(condition)!;
                model = Select.Parameters[0];
            }
            else
            {
                if (element.Type != ModelType)
                    throw new NotSupportedException($"{operation} reads keys of the elements");
                model = element;
            }
            var lambda = Expression.Lambda(ModelAnalyzer.GetModel(ModelType).LambdaDelegateType, condition, model);
            Where = Where is null ? lambda : AppendAnd(Where, lambda);
        }
        /// <summary>Combines the elements of the two queries of <see cref="Set"/>, compared by their values, and ends them as the query does.</summary>
        /// <param name="left">The elements of the first query.</param>
        /// <param name="right">The elements of the second query.</param>
        /// <param name="caseSensitive">Whether string comparisons in the condition and order after it are case sensitive.</param>
        /// <returns>The elements as a list of the result type, the count boxed as a long, or whether there are any.</returns>
        internal abstract object CombineSet(IEnumerable left, IEnumerable right, bool caseSensitive);
        /// <summary>Pairs the elements of the two queries of <see cref="Join"/> by their keys, and ends them as the query does.</summary>
        /// <param name="left">The elements of the first query.</param>
        /// <param name="right">The elements of the second query.</param>
        /// <param name="caseSensitive">Whether string keys, and string comparisons in the condition and order after it, are case sensitive.</param>
        /// <returns>The elements as a list of the result type, the count boxed as a long, or whether there are any.</returns>
        internal abstract object CombineJoin(IEnumerable left, IEnumerable? right, bool caseSensitive);

        /// <summary>Gets whether the rows are numbered in <see cref="NumberOrder"/>, for an operation by position to read.</summary>
        public bool Numbered { get; private set; }
        /// <summary>Gets the order the rows are numbered in, or <see langword="null"/> for the order the store reads them in.</summary>
        public LinqOrder[]? NumberOrder { get; private set; }
        //a query an operation by position reads gives each row its place in its order, which is then the only order it needs
        internal void ReadAsNumberedPart(string operation)
        {
            if (Graph is not null || GroupBy is not null || Combines || Distinct)
                throw new NotSupportedException($"{operation} reads the rows of a query in their order, before a GroupBy, Distinct, set operation, or join");
            if (!ReadsElements)
                throw new NotSupportedException($"{operation} reads the elements of a query");
            Numbered = true;
            NumberOrder = Order;
            if (!Skip.HasValue && !Take.HasValue)
                Order = null;
        }
        //read on its own, the rows come in the order they are numbered in
        internal void ReadInNumberOrder()
        {
            if (Numbered && Order is null)
                Order = NumberOrder;
        }

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
            if (GroupedResults)
                ResultOrder = Reversed(ResultOrder, call);
            else
                Order = Reversed(Order, call);
        }
        private static LinqOrder[] Reversed(LinqOrder[]? order, string call)
        {
            if (order is null)
                throw new NotSupportedException($"{call} needs an OrderBy to say which way round the elements are");
            var expressions = new LinqOrder[order.Length];
            for (var i = 0; i < expressions.Length; i++)
                expressions[i] = new LinqOrder(order[i].Expression, !order[i].Descending);
            return expressions;
        }

        //the terminal call a condition came in, or null for a Where
        private void Condition(LambdaExpression condition, LinqTerminal? terminal)
        {
            if (GroupedResults)
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

        //a lambda taking the element as an object reads it through a conversion
        internal static LambdaExpression AppendAnd(LambdaExpression first, LambdaExpression second)
            => Expression.Lambda(first.Type, Expression.AndAlso(first.Body, LinqRebinder.RebindExpression(second.Body, second.Parameters[0], first.Parameters[0].Type == second.Parameters[0].Type ? first.Parameters[0] : Expression.Convert(first.Parameters[0], second.Parameters[0].Type))), first.Parameters);
    }
}
