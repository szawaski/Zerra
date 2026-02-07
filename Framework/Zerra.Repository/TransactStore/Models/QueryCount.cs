// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryCount<TModel> : Query<TModel> where TModel : class, new()
    {
        public QueryCount(Expression<Func<TModel, bool>>? where = null)
          : base(QueryOperation.Count)
        {
            this.Where = where;
            this.Order = null;
            this.Skip = null;
            this.Take = null;
            this.Graph = null;
        }
    }
}
