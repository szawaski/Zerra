// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that retrieves the first matching <typeparamref name="TModel"/> record.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class QueryFirst<TModel> : Query<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance with all parameters defaulting to <see langword="null"/>.</summary>
        public QueryFirst() : this(null, null, null) { }
        /// <summary>Initializes a new instance with only a sort order; other parameters default to <see langword="null"/>.</summary>
        public QueryFirst(QueryOrder<TModel>? order) : this(null, order, null) { }
        /// <summary>Initializes a new instance with only a filter expression; other parameters default to <see langword="null"/>.</summary>
        public QueryFirst(Expression<Func<TModel, bool>>? where) : this(where, null, null) { }
        /// <summary>Initializes a new instance with a filter expression and sort order; graph defaults to <see langword="null"/>.</summary>
        public QueryFirst(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(where, order, null) { }

        /// <summary>Initializes a new instance with only a property graph; other parameters default to <see langword="null"/>.</summary>
        public QueryFirst(Graph<TModel>? graph) : this(null, null, graph) { }
        /// <summary>Initializes a new instance with a sort order and property graph; filter defaults to <see langword="null"/>.</summary>
        public QueryFirst(QueryOrder<TModel>? order, Graph<TModel> graph) : this(null, order, graph) { }
        /// <summary>Initializes a new instance with a filter expression and property graph; order defaults to <see langword="null"/>.</summary>
        public QueryFirst(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(where, null, graph) { }
        /// <summary>
        /// Initializes a new instance with full parameters.
        /// </summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional sort order.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public QueryFirst(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph)
            : base(QueryOperation.First, typeof(TModel))
        {
            this.Where = where;
            this.Order = order;
            this.Skip = null;
            this.Take = null;
            this.Graph = graph;
        }
    }
}
