// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Strongly-typed base class for a read query targeting a specific model type.
    /// </summary>
    /// <typeparam name="TModel">The type of model being queried.</typeparam>
    public class Query<TModel> : Query where TModel : class, new()
    {
        /// <summary>
        /// Initializes a new strongly-typed query with the specified operation and model type.
        /// </summary>
        /// <param name="operation">The query operation type.</param>
        /// <param name="modelType">The CLR type of the model being queried.</param>
        public Query(QueryOperation operation, Type modelType)
            : base(operation, modelType)
        {
        }

        /// <summary>
        /// Initializes a new strongly-typed query as a deep copy of an existing query.
        /// </summary>
        /// <param name="query">The query to copy.</param>
        public Query(Query<TModel> query)
            : base(query)
        {
        }
    }
}