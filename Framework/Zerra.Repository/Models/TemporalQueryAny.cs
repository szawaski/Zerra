// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class TemporalQueryAny<TModel> : QueryAny<TModel> where TModel : class, new()
    {
        public TemporalQueryAny(DateTime? temporalDateFrom, DateTime? temporalDateTo) : this(temporalDateFrom, temporalDateTo, null, null, null) { }
        public TemporalQueryAny(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>> where) : this(temporalDateFrom, temporalDateTo, null, null, where) { }

        public TemporalQueryAny(ulong? temporalNumberFrom, ulong? temporalNumberTo) : this(null, null, temporalNumberFrom, temporalNumberTo, null) { }
        public TemporalQueryAny(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>> where) : this(null, null, temporalNumberFrom, temporalNumberTo, where) { }

        public TemporalQueryAny(DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>> where)
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