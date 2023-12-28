﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository.Models
{
    public class TemporalQueryNewest<TModel> : QueryFirst<TModel> where TModel : class, new()
    {
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo) : this(temporalDateFrom, temporalDateTo, null, null, null, null, null) { }
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, QueryOrder<TModel>? order) : this(temporalDateFrom, temporalDateTo, null, null, null, order, null) { }
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where) : this(temporalDateFrom, temporalDateTo, null, null, where, null, null) { }
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(temporalDateFrom, temporalDateTo, null, null, where, order, null) { }

        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, null, null, graph) { }
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, null, order, graph) { }
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, where, null, graph) { }
        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(temporalDateFrom, temporalDateTo, null, null, where, order, graph) { }

        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo) : this(null, null, temporalNumberFrom, temporalNumberTo, null, null, null) { }
        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, QueryOrder<TModel>? order) : this(null, null, temporalNumberFrom, temporalNumberTo, null, order, null) { }
        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where) : this(null, null, temporalNumberFrom, temporalNumberTo, where, null, null) { }
        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(null, null, temporalNumberFrom, temporalNumberTo, where, order, null) { }

        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, null, null, graph) { }
        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, null, order, graph) { }
        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, where, null, graph) { }
        public TemporalQueryNewest(ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(null, null, temporalNumberFrom, temporalNumberTo, where, order, graph) { }

        public TemporalQueryNewest(DateTime? temporalDateFrom, DateTime? temporalDateTo, ulong? temporalNumberFrom, ulong? temporalNumberTo, Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph)
            : base(where, order, graph)
        {
            this.TemporalOrder = Repository.TemporalOrder.Newest;
            this.TemporalDateFrom = temporalDateFrom;
            this.TemporalDateTo = temporalDateTo;
            this.TemporalNumberFrom = temporalNumberFrom;
            this.TemporalNumberTo = temporalNumberTo;
        }
    }
}