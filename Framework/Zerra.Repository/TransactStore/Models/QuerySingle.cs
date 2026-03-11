// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QuerySingle<TModel> : Query<TModel> where TModel : class, new()
    {
        public QuerySingle() : this(null, null) { }
        public QuerySingle(Expression<Func<TModel, bool>> where) : this(where, null) { }

        public QuerySingle(Graph<TModel>? graph) : this(null, graph) { }
        public QuerySingle(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph)
            : base(QueryOperation.Single)
        {
            this.Where = where;
            this.Order = null;
            this.Skip = null;
            this.Take = null;
            this.Graph = graph;
        }
    }
}
