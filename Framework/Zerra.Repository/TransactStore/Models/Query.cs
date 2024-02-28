// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class Query<TModel> where TModel : class, new()
    {
        public QueryOperation Operation { get; }

        public Expression<Func<TModel, bool>>? Where { get; set; }
        public QueryOrder<TModel>? Order { get; set; }
        public int? Skip { get; set; }
        public int? Take { get; set; }
        public Graph<TModel>? Graph { get; set; }

        public TemporalOrder? TemporalOrder { get; set; }
        public DateTime? TemporalDateFrom { get; set; }
        public DateTime? TemporalDateTo { get; set; }
        public ulong? TemporalNumberFrom { get; set; }
        public ulong? TemporalNumberTo { get; set; }
        public int? TemporalSkip { get; set; }
        public int? TemporalTake { get; set; }

        public bool IsTemporal { get => TemporalOrder.HasValue; }

        public Query(QueryOperation operation)
        {
            this.Operation = operation;
        }

        public Query(Query<TModel> query)
        {
            if (query != null)
            {
                this.Operation = query.Operation;
                this.TemporalOrder = query.TemporalOrder;
                this.TemporalDateFrom = query.TemporalDateFrom;
                this.TemporalDateTo = query.TemporalDateTo;
                this.TemporalSkip = query.TemporalSkip;
                this.TemporalTake = query.TemporalTake;
                this.Where = query.Where;
                this.Order = query.Order;
                this.Skip = query.Skip;
                this.Take = query.Take;
                this.Graph = query.Graph == null ? null : new Graph<TModel>(query.Graph);
            }
        }
    }
}