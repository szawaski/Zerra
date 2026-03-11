// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryMany<TModel> : Query<TModel> where TModel : class, new()
    {
        public QueryMany() : this(null, null, null, null, null) { }
        public QueryMany(QueryOrder<TModel>? order) : this(null, order, null, null, null) { }
        public QueryMany(QueryOrder<TModel>? order, int? skip, int? take) : this(null, order, skip, take, null) { }
        public QueryMany(Expression<Func<TModel, bool>>? where) : this(where, null, null, null, null) { }
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(where, order, null, null, null) { }
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take) : this(where, order, skip, take, null) { }

        public QueryMany(Graph<TModel>? graph) : this(null, null, null, null, graph) { }
        public QueryMany(QueryOrder<TModel>? order, Graph<TModel>? graph) : this(null, order, null, null, graph) { }
        public QueryMany(QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph) : this(null, order, skip, take, graph) { }
        public QueryMany(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(where, null, null, null, graph) { }
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(where, order, null, null, graph) { }
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph)
            : base(QueryOperation.Many)
        {
            this.Where = where;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
            this.Graph = graph;
        }
    }
}
