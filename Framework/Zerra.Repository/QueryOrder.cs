// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Represents an abstract ordering definition used to specify sort criteria for a query.
    /// </summary>
    public abstract class QueryOrder
    {
        /// <summary>
        /// Gets the array of <see cref="OrderExpression"/> instances that define the sort criteria.
        /// </summary>
        public OrderExpression[] OrderExpressions { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="QueryOrder"/> with the specified ordering expressions.
        /// </summary>
        /// <param name="expressions">The ordering expressions that define the sort criteria.</param>
        public QueryOrder(OrderExpression[] expressions)
        {
            this.OrderExpressions = expressions;
        }

        /// <summary>
        /// Returns a string representation of all ordering expressions.
        /// </summary>
        /// <returns>A comma-separated string of the ordering expressions.</returns>
        public override string ToString()
        {
            return String.Join(",", this.OrderExpressions.Select(x => x.ToString()));
        }
    }
}

