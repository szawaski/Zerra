// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public sealed class EventQueryMany<TModel> : Query<TModel> where TModel : class, new()
    {
        public EventQueryMany(TemporalOrder? temporalOrder = null, DateTime? temporalDateFrom = null, DateTime? temporalDateTo = null, ulong? temporalNumberFrom = null, ulong? temporalNumberTo = null, int? temporalSkip = null, int? temporalTake = null, Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null, Graph<TModel>? graph = null)
            : base(QueryOperation.EventMany)
        {
            this.TemporalOrder = temporalOrder ?? Repository.TemporalOrder.Newest;
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
