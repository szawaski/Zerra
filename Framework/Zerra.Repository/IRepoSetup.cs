// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Extends <see cref="IRepo"/> with setup operations for configuring data providers.
    /// </summary>
    public interface IRepoSetup : IRepo
    {
        /// <summary>
        /// Registers a transact store provider for the specified model type.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="provider">The provider to register for <typeparamref name="TModel"/>.</param>
        void AddProvider<TModel>(ITransactStoreProvider<TModel> provider) where TModel : class, new();
    }
}
