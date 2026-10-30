// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Represents the full set of parameters for a read query, including filter, ordering, pagination,
    /// property graph, and optional temporal (event-history) scoping.
    /// </summary>
    public class Query
    {
        /// <summary>Gets the query operation type.</summary>
        public QueryOperation Operation { get; }

        /// <summary>Gets the CLR type of the model being queried.</summary>
        public Type ModelType { get; }

        /// <summary>Gets or sets the optional filter expression applied to model records.</summary>
        public LambdaExpression? Where { get; set; }
        /// <summary>Gets or sets the optional sort order for results.</summary>
        public QueryOrder? Order { get; set; }
        /// <summary>Gets the number of records to skip for pagination.</summary>
        public int? Skip { get; init; }
        /// <summary>Gets the maximum number of records to return.</summary>
        public int? Take { get; init; }
        /// <summary>Gets the property graph specifying which model members to populate.</summary>
        public Graph? Graph { get; init; }

        /// <summary>Gets the expression selecting the group key for a grouped calculation, or <see langword="null"/> for a calculation over all matching records.</summary>
        public LambdaExpression? GroupBy { get; init; }
        /// <summary>Gets the expression selecting the value a calculation adds, averages, or compares, or <see langword="null"/> for a count.</summary>
        public LambdaExpression? Value { get; init; }
        /// <summary>Gets the projection building a result from each record, or from each group with <see cref="GroupBy"/>; <see langword="null"/> for other queries.</summary>
        public LambdaExpression? Select { get; init; }
        /// <summary>Gets the optional filter applied to the projected results of a grouped query. <see cref="Order"/>, <see cref="Skip"/>, and <see cref="Take"/> apply to those results too.</summary>
        public LambdaExpression? Having { get; init; }

        /// <summary>Gets the ordering direction for temporal queries; <see langword="null"/> indicates this is not a temporal query.</summary>
        public TemporalOrder? TemporalOrder { get; init; }
        /// <summary>Gets the inclusive start date for temporal range filtering.</summary>
        public DateTime? TemporalDateFrom { get; init; }
        /// <summary>Gets the inclusive end date for temporal range filtering.</summary>
        public DateTime? TemporalDateTo { get; init; }
        /// <summary>Gets the inclusive start event number for temporal range filtering.</summary>
        public ulong? TemporalNumberFrom { get; init; }
        /// <summary>Gets the inclusive end event number for temporal range filtering.</summary>
        public ulong? TemporalNumberTo { get; init; }
        /// <summary>Gets the number of temporal events to skip.</summary>
        public int? TemporalSkip { get; init; }
        /// <summary>Gets the maximum number of temporal events to return.</summary>
        public int? TemporalTake { get; init; }

        /// <summary>Gets a value indicating whether this is a temporal (event-history) query.</summary>
        public bool IsTemporal { get => TemporalOrder.HasValue; }

        /// <summary>
        /// Initializes a new query with the specified operation and model type.
        /// </summary>
        /// <param name="operation">The query operation type.</param>
        /// <param name="modelType">The CLR type of the model being queried.</param>
        public Query(QueryOperation operation, Type modelType)
        {
            this.Operation = operation;
            this.ModelType = modelType;
        }

        /// <summary>
        /// Initializes a new query as a deep copy of an existing query.
        /// </summary>
        /// <param name="query">The query to copy.</param>
        public Query(Query query)
        {
            this.Operation = query.Operation;
            this.ModelType = query.ModelType;
            this.TemporalOrder = query.TemporalOrder;
            this.TemporalDateFrom = query.TemporalDateFrom;
            this.TemporalDateTo = query.TemporalDateTo;
            this.TemporalNumberFrom = query.TemporalNumberFrom;
            this.TemporalNumberTo = query.TemporalNumberTo;
            this.TemporalSkip = query.TemporalSkip;
            this.TemporalTake = query.TemporalTake;
            this.Where = query.Where;
            this.Order = query.Order;
            this.Skip = query.Skip;
            this.Take = query.Take;
            this.Graph = query.Graph is null ? null : new Graph(query.Graph);
            this.GroupBy = query.GroupBy;
            this.Value = query.Value;
            this.Select = query.Select;
            this.Having = query.Having;
        }

        /// <summary>
        /// Initializes a standard (non-temporal) query with full parameters.
        /// </summary>
        /// <param name="operation">The query operation type.</param>
        /// <param name="modelType">The CLR type of the model being queried.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional sort order.</param>
        /// <param name="skip">The number of records to skip for pagination.</param>
        /// <param name="take">The maximum number of records to return.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public Query(QueryOperation operation, Type modelType, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph)
        {
            this.Operation = operation;
            this.ModelType = modelType;

            this.Where = where;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
            this.Graph = graph;
        }

        /// <summary>
        /// Initializes a temporal query with full parameters.
        /// </summary>
        /// <param name="operation">The query operation type.</param>
        /// <param name="modelType">The CLR type of the model being queried.</param>
        /// <param name="temporalOrder">The temporal ordering direction.</param>
        /// <param name="temporalDateFrom">The inclusive start date for temporal range filtering.</param>
        /// <param name="temporalDateTo">The inclusive end date for temporal range filtering.</param>
        /// <param name="temporalNumberFrom">The inclusive start event number for temporal range filtering.</param>
        /// <param name="temporalNumberTo">The inclusive end event number for temporal range filtering.</param>
        /// <param name="temporalSkip">The number of temporal events to skip.</param>
        /// <param name="temporalTake">The maximum number of temporal events to return.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional sort order.</param>
        /// <param name="skip">The number of records to skip for pagination.</param>
        /// <param name="take">The maximum number of records to return.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public Query(QueryOperation operation, Type modelType, TemporalOrder? temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph)
        {
            this.Operation = operation;
            this.ModelType = modelType;

            this.TemporalOrder = temporalOrder;
            this.TemporalDateFrom = temporalDateFrom;
            this.TemporalDateTo = temporalDateTo;
            this.TemporalNumberFrom = temporalNumberFrom;
            this.TemporalNumberTo = temporalNumberTo;
            this.TemporalSkip = temporalSkip;
            this.TemporalTake = temporalTake;

            this.Where = where;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
            this.Graph = graph;
        }

        /// <summary>
        /// Initializes a calculation query, over all matching records or per group.
        /// </summary>
        /// <param name="operation">The calculation: <see cref="QueryOperation.Count"/>, <see cref="QueryOperation.Sum"/>, <see cref="QueryOperation.Average"/>, <see cref="QueryOperation.Min"/>, or <see cref="QueryOperation.Max"/>.</param>
        /// <param name="modelType">The CLR type of the model being queried.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="groupBy">The group key expression, or <see langword="null"/> to calculate over all matching records.</param>
        /// <param name="value">The value expression, or <see langword="null"/> for a count.</param>
        public Query(QueryOperation operation, Type modelType, LambdaExpression? where, LambdaExpression? groupBy, LambdaExpression? value)
        {
            this.Operation = operation;
            this.ModelType = modelType;

            this.Where = where;
            this.GroupBy = groupBy;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a projection query, of each record or of each group.
        /// </summary>
        /// <param name="operation"><see cref="QueryOperation.Select"/> or <see cref="QueryOperation.Group"/>.</param>
        /// <param name="modelType">The CLR type of the model being queried.</param>
        /// <param name="where">An optional filter expression on the records.</param>
        /// <param name="groupBy">The group key expression for <see cref="QueryOperation.Group"/>, otherwise <see langword="null"/>.</param>
        /// <param name="select">The projection building a result from each record or group.</param>
        /// <param name="having">An optional filter on the results of a grouped query.</param>
        /// <param name="order">An optional sort order, on the records for a select and on the results for a group.</param>
        /// <param name="skip">The number of records or results to skip.</param>
        /// <param name="take">The maximum number of records or results to return.</param>
        public Query(QueryOperation operation, Type modelType, LambdaExpression? where, LambdaExpression? groupBy, LambdaExpression select, LambdaExpression? having, QueryOrder? order, int? skip, int? take)
        {
            this.Operation = operation;
            this.ModelType = modelType;

            this.Where = where;
            this.GroupBy = groupBy;
            this.Select = select;
            this.Having = having;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
        }

        /// <summary>
        /// Runs a query whose results are built as a type other than the model, a projection, the different values of a member, or a calculation per group, on an engine with the result types the query was made with.
        /// A query of models, or of one calculation over all of them, is run by the provider itself.
        /// </summary>
        /// <param name="engine">The engine of the model's store.</param>
        /// <param name="modelDetail">The model's detail.</param>
        /// <returns>The results.</returns>
        internal virtual object? Execute(ITransactStoreEngine engine, ModelDetail modelDetail) => throw new NotSupportedException($"{Operation} is run by the provider itself");
        /// <summary>
        /// Asynchronously runs a query whose results are built as a type other than the model on an engine with the result types the query was made with.
        /// </summary>
        /// <param name="engine">The engine of the model's store.</param>
        /// <param name="modelDetail">The model's detail.</param>
        /// <returns>A task containing the results.</returns>
        internal virtual Task<object?> ExecuteAsync(ITransactStoreEngine engine, ModelDetail modelDetail) => throw new NotSupportedException($"{Operation} is run by the provider itself");
        /// <summary>
        /// Runs a query whose results are built as a type other than the model over models already loaded, with the result types the query was made with.
        /// The models are those <see cref="Where"/> matches, and for <see cref="QueryOperation.Select"/> already in <see cref="Order"/> with <see cref="Skip"/> and <see cref="Take"/> applied; a group's order and paging apply to its results.
        /// </summary>
        /// <param name="models">The models.</param>
        /// <returns>The results.</returns>
        internal virtual object? Execute(IEnumerable<object> models) => throw new NotSupportedException($"{Operation} is run by the provider itself");
    }
}
