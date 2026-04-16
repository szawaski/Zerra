// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;
using Zerra.Linq;

namespace Zerra.Repository
{
    public class Query
    {
        public QueryOperation Operation { get; }

        public Type ModelType { get; }

        public LambdaExpression? Where { get; set; }
        public QueryOrder? Order { get; set; }
        public int? Skip { get; init; }
        public int? Take { get; init; }
        public Graph? Graph { get; init; }

        public TemporalOrder? TemporalOrder { get; init; }
        public DateTime? TemporalDateFrom { get; init; }
        public DateTime? TemporalDateTo { get; init; }
        public ulong? TemporalNumberFrom { get; init; }
        public ulong? TemporalNumberTo { get; init; }
        public int? TemporalSkip { get; init; }
        public int? TemporalTake { get; init; }

        public bool IsTemporal { get => TemporalOrder.HasValue; }

        public Query(QueryOperation operation, Type modelType)
        {
            this.Operation = operation;
            this.ModelType = modelType;
        }

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