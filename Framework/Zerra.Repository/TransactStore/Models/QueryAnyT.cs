// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that checks whether any <typeparamref name="TModel"/> records satisfy the optional filter.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class QueryAny<TModel> : Query<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance with the filter defaulting to <see langword="null"/>.</summary>
        public QueryAny() : this(null) { }
        /// <summary>
        /// Initializes a new instance with the specified filter expression.
        /// </summary>
        /// <param name="where">An optional filter expression.</param>
        public QueryAny(Expression<Func<TModel, bool>>? where)
            : base(QueryOperation.Any, typeof(TModel))
        {
            this.Where = where;
            this.Order = null;
            this.Skip = null;
            this.Take = null;
            this.Graph = null;
        }
    }
}
