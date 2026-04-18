// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Diagnostics.CodeAnalysis;

namespace Zerra.Repository
{
    public sealed partial class Repo : IRepoSetup, IRepoInternal
    {
        private readonly RepoContext context;
        private readonly Dictionary<Type, ITransactStoreProvider> providers;

        public static IRepoSetup New()
        {
            var repo = new Repo();
            Repo.staticRepo = repo;
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

        IReadOnlyCollection<TModel> IRepo.Query<TModel>(QueryMany<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (IReadOnlyCollection<TModel>)(provider.Query(query))!;
        }
        TModel? IRepo.Query<TModel>(QueryFirst<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (TModel?)(provider.Query(query));
        }
        TModel? IRepo.Query<TModel>(QuerySingle<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (TModel?)(provider.Query(query));
        }
        bool IRepo.Query<TModel>(QueryAny<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (bool)(provider.Query(query))!;
        }
        long IRepo.Query<TModel>(QueryCount<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (long)(provider.Query(query))!;
        }
        IReadOnlyCollection<EventModel<TModel>> IRepo.Query<TModel>(EventQueryMany<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (IReadOnlyCollection<EventModel<TModel>>)(provider.Query(query))!;
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

        async Task<IReadOnlyCollection<TModel>> IRepo.QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (IReadOnlyCollection<TModel>)(await provider.QueryAsync(query))!;
        }
        async Task<TModel?> IRepo.QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (TModel?)(await provider.QueryAsync(query));
        }
        async Task<TModel?> IRepo.QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (TModel?)(await provider.QueryAsync(query));
        }
        async Task<bool> IRepo.QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (bool)(await provider.QueryAsync(query))!;
        }
        async Task<long> IRepo.QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (long)(await provider.QueryAsync(query))!;
        }
        async Task<IReadOnlyCollection<EventModel<TModel>>> IRepo.QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class
        {
            if (!providers.TryGetValue(query.ModelType, out var provider))
                throw new Exception($"No provider for {query.ModelType.FullName}");
            return (IReadOnlyCollection<EventModel<TModel>>)(await provider.QueryAsync(query))!;
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
