// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Strongly-typed base class for a read query targeting a specific model type.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class Query<TModel> : Query where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new strongly-typed query as a deep copy of an existing query.
        /// </summary>
        /// <param name="query">The query to copy.</param>
        public Query(Query<TModel> query)
            : base(query)
        {
        }

        /// <summary>
        /// Initializes a standard (non-temporal) query with full parameters.
        /// </summary>
        /// <param name="operation">The query operation type.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional sort order.</param>
        /// <param name="skip">The number of records to skip for pagination.</param>
        /// <param name="take">The maximum number of records to return.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public Query(QueryOperation operation, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph)
            : base(operation, typeof(TModel), where, order, skip, take, graph)
        {

        }

        /// <summary>
        /// Initializes a temporal query with full parameters.
        /// </summary>
        /// <param name="operation">The query operation type.</param>
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
        public Query(QueryOperation operation, TemporalOrder? temporalOrder, DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, int? temporalSkip, int? temporalTake, LambdaExpression? where, QueryOrder? order, int? skip, int? take, Graph? graph)
            : base(operation, typeof(TModel), temporalOrder, temporalDateFrom, temporalDateTo, temporalNumberFrom, temporalNumberTo, temporalSkip, temporalTake, where, order, skip, take, graph)
        {
            
        }
    }
}