// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that retrieves exactly one matching <typeparamref name="TModel"/> record.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class QuerySingle<TModel> : Query<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance with all parameters defaulting to <see langword="null"/>.</summary>
        public QuerySingle() : this(null, null) { }
        /// <summary>Initializes a new instance with only a filter expression; graph defaults to <see langword="null"/>.</summary>
        public QuerySingle(Expression<Func<TModel, bool>> where) : this(where, null) { }

        /// <summary>Initializes a new instance with only a property graph; filter defaults to <see langword="null"/>.</summary>
        public QuerySingle(Graph<TModel>? graph) : this(null, graph) { }
        /// <summary>
        /// Initializes a new instance with full parameters.
        /// </summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public QuerySingle(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph)
            : base(QueryOperation.Single, typeof(TModel))
        {
            this.Where = where;
            this.Order = null;
            this.Skip = null;
            this.Take = null;
            this.Graph = graph;
        }
    }
}
