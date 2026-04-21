// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Extends <see cref="IProviderRelation"/> with strongly-typed post-retrieve processing for <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public interface IProviderRelation<TModel> : IProviderRelation
        where TModel : class, new()
    {
        /// <summary>Post-processes a strongly-typed collection of retrieved models through the provider chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>The processed models.</returns>
        IReadOnlyCollection<TModel> OnGetIncludingBase(IReadOnlyCollection<TModel> models, Graph? graph);
        /// <summary>Asynchronously post-processes a strongly-typed collection of retrieved models through the provider chain.</summary>
        /// <param name="models">The retrieved models.</param>
        /// <param name="graph">The graph used during the query.</param>
        /// <returns>A task containing the processed models.</returns>
        Task<IReadOnlyCollection<TModel>> OnGetIncludingBaseAsync(IReadOnlyCollection<TModel> models, Graph? graph);
    }
}
