// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Linq.Expressions;

namespace Zerra.Repository
{
    /// <summary>
    /// Defines relation-linking hooks for query preparation and post-retrieve processing in a provider chain.
    /// </summary>
    public interface IProviderRelation
    {
        /// <summary>Propagates the query event through the provider chain, allowing each layer to augment the graph.</summary>
        /// <param name="graph">The graph for the current query.</param>
        void OnQueryIncludingBase(Graph? graph);

        /// <summary>Post-processes retrieved models through the provider chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>The processed models.</returns>
        IEnumerable OnGetIncludingBase(IEnumerable models, Graph? graph);
        /// <summary>Asynchronously post-processes retrieved models through the provider chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>A task containing the processed models.</returns>
        Task<IEnumerable> OnGetIncludingBaseAsync(IEnumerable models, Graph? graph);

        /// <summary>Returns the combined where expression from all providers in the chain.</summary>
        /// <param name="graph">The graph for the current query.</param>
        /// <returns>A combined lambda where expression, or <see langword="null"/> if none applies.</returns>
        LambdaExpression? GetWhereExpressionIncludingBase(Graph? graph);
    }
}
