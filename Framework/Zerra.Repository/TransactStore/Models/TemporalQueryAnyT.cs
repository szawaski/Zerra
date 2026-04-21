// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// A temporal query that checks whether any <typeparamref name="TModel"/> event records exist
    /// within an optional date or event-number range.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class TemporalQueryAny<TModel> : QueryAny<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance scoped to a date range; filter defaults to <see langword="null"/>.</summary>
        public TemporalQueryAny(DateTime? temporalDateFrom, DateTime? temporalDateTo) : this(temporalDateFrom, temporalDateTo, null, null, null) { }
        /// <summary>Initializes a new instance scoped to a date range with a filter expression.</summary>
        public TemporalQueryAny(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>> where) : this(temporalDateFrom, temporalDateTo, null, null, where) { }

        /// <summary>Initializes a new instance scoped to an event-number range; filter defaults to <see langword="null"/>.</summary>
        public TemporalQueryAny(ulong? temporalNumberFrom, ulong? temporalNumberTo) : this(null, null, temporalNumberFrom, temporalNumberTo, null) { }
        /// <summary>Initializes a new instance scoped to an event-number range with a filter expression.</summary>
        public TemporalQueryAny(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where) : this(null, null, temporalNumberFrom, temporalNumberTo, where) { }

        /// <summary>
        /// Initializes a new instance with full temporal and filter parameters.
        /// </summary>
        /// <param name="temporalDateFrom">The inclusive start date for temporal range filtering.</param>
        /// <param name="temporalDateTo">The inclusive end date for temporal range filtering.</param>
        /// <param name="temporalNumberFrom">The inclusive start event number for temporal range filtering.</param>
        /// <param name="temporalNumberTo">The inclusive end event number for temporal range filtering.</param>
        /// <param name="where">An optional filter expression.</param>
        public TemporalQueryAny(DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where)
            : base(where)
        {
            this.TemporalOrder = Repository.TemporalOrder.Newest;
            this.TemporalDateFrom = temporalDateFrom;
            this.TemporalDateTo = temporalDateTo;
            this.TemporalNumberFrom = temporalNumberFrom;
            this.TemporalNumberTo = temporalNumberTo;
        }
    }
}