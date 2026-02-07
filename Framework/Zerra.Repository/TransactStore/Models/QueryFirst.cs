// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryFirst<TModel> : Query<TModel> where TModel : class, new()
    {
        public QueryFirst(Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, Graph<TModel>? graph = null)
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
