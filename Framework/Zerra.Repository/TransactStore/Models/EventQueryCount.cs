// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository.Models
{
    public sealed class EventQueryCount<TModel> : Query<TModel> where TModel : class, new()
    {
        public EventQueryCount(DateTime? temporalDateFrom, DateTime? temporalDateTo) : this(temporalDateFrom, temporalDateTo, null, null, null) { }
        public EventQueryCount(DateTime? temporalDateFrom, DateTime? temporalDateTo, QueryOrder<TModel> order) : this(temporalDateFrom, temporalDateTo, null, null, null) { }
        public EventQueryCount(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>> where) : this(temporalDateFrom, temporalDateTo, null, null, where) { }

        public EventQueryCount(ulong? temporalNumberFrom, ulong? temporalNumberTo) : this(null, null, temporalNumberFrom, temporalNumberTo, null) { }
        public EventQueryCount(ulong? temporalNumberFrom, ulong? temporalNumberTo, QueryOrder<TModel> order) : this(null, null, temporalNumberFrom, temporalNumberTo, null) { }
        public EventQueryCount(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>> where) : this(null, null, temporalNumberFrom, temporalNumberTo, where) { }

        public EventQueryCount(DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>> where)
            : base(QueryOperation.EventCount)
        {
            this.TemporalOrder = Repository.TemporalOrder.Newest;
            this.TemporalDateFrom = temporalDateFrom;
            this.TemporalDateTo = temporalDateTo;
            this.TemporalNumberFrom = temporalNumberFrom;
            this.TemporalNumberTo = temporalNumberTo;

            this.Where = where;
        }
    }
}