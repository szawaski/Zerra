// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryMany<TModel> : Query<TModel> where TModel : class, new()
    {
        public QueryMany(Expression<Func<TModel, bool>>? where = null, QueryOrder<TModel>? order = null, int? skip = null, int? take = null, Graph<TModel>? graph = null)
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
