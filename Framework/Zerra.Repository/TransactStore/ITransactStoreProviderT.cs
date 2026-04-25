// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Strongly-typed marker interface for a transact store provider that manages <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model type managed by this provider.</typeparam>
    public interface ITransactStoreProvider<TModel> : ITransactStoreProvider where TModel : class, new()
    {
    }
}
