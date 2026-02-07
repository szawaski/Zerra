// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QuerySingle<TModel> : Query<TModel> where TModel : class, new()
    {
        public QuerySingle(Expression<Func<TModel, bool>>? where = null, Graph<TModel>? graph = null)
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
