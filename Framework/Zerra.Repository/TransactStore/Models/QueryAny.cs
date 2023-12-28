// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    public class QueryAny<TModel> : Query<TModel> where TModel : class, new()
    {
        public QueryAny() : this(null) { }
        public QueryAny(Expression<Func<TModel, bool>>? where)
            : base(QueryOperation.Any)
        {
            this.Where = where;
            this.Order = null;
            this.Skip = null;
            this.Take = null;
            this.Graph = null;
        }
    }
}
