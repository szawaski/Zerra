// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    /// <summary>
    /// Internal extension of <see cref="IRepo"/> that exposes provider resolution for cross-repo operations.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface IRepoInternal : IRepo
    {
        /// <summary>
        /// Attempts to retrieve the <see cref="ITransactStoreProvider"/> registered for the specified model type.
        /// </summary>
        /// <param name="modelType">The model type to look up.</param>
        /// <param name="provider">When this method returns <see langword="true"/>, contains the matching <see cref="ITransactStoreProvider"/>; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a provider was found for <paramref name="modelType"/>; otherwise, <see langword="false"/>.</returns>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        bool TryGetProvider(Type modelType, [MaybeNullWhen(false)] out ITransactStoreProvider provider);
    }
}