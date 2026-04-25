// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    /// <summary>
    /// Default implementation of <see cref="IRepoSetup"/> that dispatches queries and persist operations to registered <see cref="ITransactStoreProvider{TModel}"/> instances.
    /// </summary>
    public sealed partial class Repo : IRepoSetup, IRepoInternal
    {
        private readonly RepoContext context;
        private readonly Dictionary<Type, ITransactStoreProvider> providers;

        /// <summary>
        /// Creates a new <see cref="Repo"/> instance and sets it as the static repository.
        /// </summary>
        /// <returns>The new <see cref="IRepoSetup"/> instance.</returns>
        public static IRepoSetup New()
        {
            var repo = new Repo();
            return repo;
        }

        private Repo()
        {
            this.context = new RepoContext(this);
            this.providers = new();
        }

        object? IRepo.Query(Query query)
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return provider.Query(query);
        }

        void IRepo.Persist(Persist persist)
        {
            if (!providers.TryGetValue(persist.ModelType, out var provider))
                throw new Exception($"No provider for {persist.ModelType.FullName}");
            provider.Persist(persist);
        }

        Task<object?> IRepo.QueryAsync(Query query)
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return provider.QueryAsync(query);
        }

        Task IRepo.PersistAsync(Persist persist)
        {
            if (!providers.TryGetValue(persist.ModelType, out var provider))
                throw new Exception($"No provider for {persist.ModelType.FullName}");
            return provider.PersistAsync(persist);
        }

        void IRepoSetup.AddProvider<TModel>(ITransactStoreProvider<TModel> provider)
        {
            var modelType = typeof(TModel);
            if (!providers.TryAdd(modelType, provider))
                throw new Exception($"Provider for {modelType.FullName} already exists");
            if (provider is BaseStore baseStore)
                baseStore.Initialize(context);
        }

        bool IRepoInternal.TryGetProvider(Type modelType, [MaybeNullWhen(false)] out ITransactStoreProvider provider)
        {
            return providers.TryGetValue(modelType, out provider);
        }
    }
}
