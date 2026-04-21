// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// A query that retrieves multiple <typeparamref name="TModel"/> records,
    /// with optional filter, ordering, pagination, and property graph.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class QueryMany<TModel> : Query<TModel> where TModel : class, new()
    {
        /// <summary>Initializes a new instance with all parameters defaulting to <see langword="null"/>.</summary>
        public QueryMany() : this(null, null, null, null, null) { }
        /// <summary>Initializes a new instance with only a sort order; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(QueryOrder<TModel>? order) : this(null, order, null, null, null) { }
        /// <summary>Initializes a new instance with a sort order and pagination; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(QueryOrder<TModel>? order, int? skip, int? take) : this(null, order, skip, take, null) { }
        /// <summary>Initializes a new instance with only a filter expression; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(Expression<Func<TModel, bool>>? where) : this(where, null, null, null, null) { }
        /// <summary>Initializes a new instance with a filter expression and sort order; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order) : this(where, order, null, null, null) { }
        /// <summary>Initializes a new instance with a filter expression, sort order, and pagination; graph defaults to <see langword="null"/>.</summary>
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take) : this(where, order, skip, take, null) { }

        /// <summary>Initializes a new instance with only a property graph; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(Graph<TModel>? graph) : this(null, null, null, null, graph) { }
        /// <summary>Initializes a new instance with a sort order and property graph; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(QueryOrder<TModel>? order, Graph<TModel>? graph) : this(null, order, null, null, graph) { }
        /// <summary>Initializes a new instance with a sort order, pagination, and property graph; filter defaults to <see langword="null"/>.</summary>
        public QueryMany(QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph) : this(null, order, skip, take, graph) { }
        /// <summary>Initializes a new instance with a filter expression and property graph; other parameters default to <see langword="null"/>.</summary>
        public QueryMany(Expression<Func<TModel, bool>>? where, Graph<TModel>? graph) : this(where, null, null, null, graph) { }
        /// <summary>Initializes a new instance with a filter expression, sort order, and property graph; pagination defaults to <see langword="null"/>.</summary>
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, Graph<TModel>? graph) : this(where, order, null, null, graph) { }
        /// <summary>
        /// Initializes a new instance with full parameters.
        /// </summary>
        /// <param name="where">An optional filter expression.</param>
        /// <param name="order">An optional sort order.</param>
        /// <param name="skip">The number of records to skip for pagination.</param>
        /// <param name="take">The maximum number of records to return.</param>
        /// <param name="graph">The property graph specifying which members to populate.</param>
        public QueryMany(Expression<Func<TModel, bool>>? where, QueryOrder<TModel>? order, int? skip, int? take, Graph<TModel>? graph)
            : base(QueryOperation.Many, typeof(TModel))
        {
            this.Where = where;
            this.Order = order;
            this.Skip = skip;
            this.Take = take;
            this.Graph = graph;
        }
    }
}
