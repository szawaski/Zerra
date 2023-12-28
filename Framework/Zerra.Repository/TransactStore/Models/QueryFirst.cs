// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryFirst<TModel> : Query<TModel> where TModel : class, new()
    {
        public QueryFirst() : this(null, null, null) { }
        public QueryFirst(QueryOrder<TModel>? order) : this(null, order, null) { }
        public QueryFirst(Expression<Func<TModel, bool>>? where) : this(where, null, null) { }
        public QueryFirst(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(where, order, null) { }

        public QueryFirst(Graph<TModel>? graph) : this(null, null, graph) { }
        public QueryFirst(QueryOrder<TModel>? order, Graph<TModel> graph) : this(null, order, graph) { }
        public QueryFirst(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(where, null, graph) { }
        public QueryFirst(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph)
            : base(QueryOperation.First)
        {
            this.Where = where;
            this.Order = order;
            this.Skip = null;
            this.Take = null;
            this.Graph = graph;
        }
    }
}
