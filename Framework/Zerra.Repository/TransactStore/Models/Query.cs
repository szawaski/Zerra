// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

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
            this.TemporalSkip = query.TemporalSkip;
            this.TemporalTake = query.TemporalTake;
            this.Where = query.Where;
            this.Order = query.Order;
            this.Skip = query.Skip;
            this.Take = query.Take;
            this.Graph = query.Graph is null ? null : new Graph(query.Graph);
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
    }
}