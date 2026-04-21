// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository.Models
{
    /// <summary>
    /// A query against the event store that retrieves the most-recent <typeparamref name="TModel"/> event records
    /// within an optional date or event-number range.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public sealed class EventQueryNewest<TModel> : Query<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance scoped to a date range; filter, order, and graph default to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo) : this(temporalDateFrom, temporalDateTo, null, null, null, null, null) { }
        /// <summary>Initializes a new instance scoped to a date range with a sort order; other parameters default to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, QueryOrder<TModel>? order) : this(temporalDateFrom, temporalDateTo, null, null, null, order, null) { }
        /// <summary>Initializes a new instance scoped to a date range with a filter expression; other parameters default to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where) : this(temporalDateFrom, temporalDateTo, null, null, where, null, null) { }
        /// <summary>Initializes a new instance scoped to a date range with a filter expression and sort order; graph defaults to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(temporalDateFrom, temporalDateTo, null, null, where, order, null) { }

        /// <summary>Initializes a new instance scoped to a date range with a property graph; other parameters default to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, null, null, graph) { }
        /// <summary>Initializes a new instance scoped to a date range with a sort order and property graph; filter defaults to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, null, order, graph) { }
        /// <summary>Initializes a new instance scoped to a date range with a filter expression and property graph; order defaults to <see langword="null"/>.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, where, null, graph) { }
        /// <summary>Initializes a new instance scoped to a date range with full filter, order, and graph parameters.</summary>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, where, order, graph) { }

        /// <summary>Initializes a new instance scoped to an event-number range; filter, order, and graph default to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo) : this(null, null, temporalNumberFrom, temporalNumberTo, null, null, null) { }
        /// <summary>Initializes a new instance scoped to an event-number range with a sort order; other parameters default to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, QueryOrder<TModel>? order) : this(null, null, temporalNumberFrom, temporalNumberTo, null, order, null) { }
        /// <summary>Initializes a new instance scoped to an event-number range with a filter expression; other parameters default to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where) : this(null, null, temporalNumberFrom, temporalNumberTo, where, null, null) { }
        /// <summary>Initializes a new instance scoped to an event-number range with a filter expression and sort order; graph defaults to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(null, null, temporalNumberFrom, temporalNumberTo, where, order, null) { }

        /// <summary>Initializes a new instance scoped to an event-number range with a property graph; other parameters default to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, null, null, graph) { }
        /// <summary>Initializes a new instance scoped to an event-number range with a sort order and property graph; filter defaults to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, null, order, graph) { }
        /// <summary>Initializes a new instance scoped to an event-number range with a filter expression and property graph; order defaults to <see langword="null"/>.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, where, null, graph) { }
        /// <summary>Initializes a new instance scoped to an event-number range with full filter, order, and graph parameters.</summary>
        public EventQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, where, order, graph) { }

        /// <summary>
        /// Initializes a new instance with full temporal and query parameters.
        /// </summary>
        /// <param name="temporalDateFrom">The inclusive start date for temporal range filtering.</param>
        /// <param name="temporalDateTo">The inclusive end date for temporal range filtering.</param>
        /// <param name="temporalNumberFrom">The inclusive start event number for temporal range filtering.</param>
        /// <param name="temporalNumberTo">The inclusive end event number for temporal range filtering.</param>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional sort order.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public EventQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph)
            : base(QueryOperation.EventMany, typeof(TModel))
        {
            this.TemporalOrder = Repository.TemporalOrder.Newest;
            this.TemporalDateFrom = temporalDateFrom;
            this.TemporalDateTo = temporalDateTo;
            this.TemporalNumberFrom = temporalNumberFrom;
            this.TemporalNumberTo = temporalNumberTo;

            this.Where = where;
            this.Order = order;
            this.Graph = graph;
        }
    }
}