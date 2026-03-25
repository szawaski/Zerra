// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public sealed partial class Repo : IRepoSetup
    {
        private readonly Dictionary<Type, object> providers;

        public static IRepoSetup New()
        {
            var repo = new Repo();
            Repo.staticRepo = repo;
            return repo;
        }

        private Repo()
        {
            this.providers = new();
        }

        IReadOnlyCollection<TModel> IRepo.Query<TModel>(QueryMany<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (IReadOnlyCollection<TModel>)(provider.Query(query))!;
        }
        TModel? IRepo.Query<TModel>(QueryFirst<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (TModel?)(provider.Query(query));
        }
        TModel? IRepo.Query<TModel>(QuerySingle<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (TModel?)(provider.Query(query));
        }
        bool IRepo.Query<TModel>(QueryAny<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (bool)(provider.Query(query))!;
        }
        long IRepo.Query<TModel>(QueryCount<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (long)(provider.Query(query))!;
        }
        IReadOnlyCollection<EventModel<TModel>> IRepo.Query<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (IReadOnlyCollection<EventModel<TModel>>)(provider.Query(query))!;
        }

        void IRepo.Persist<TModel>(Persist<TModel> persist) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            provider.Persist(persist);
        }

        async Task<IReadOnlyCollection<TModel>> IRepo.QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (IReadOnlyCollection<TModel>)(await provider.QueryAsync(query))!;
        }
        async Task<TModel?> IRepo.QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (TModel?)(await provider.QueryAsync(query));
        }
        async Task<TModel?> IRepo.QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (TModel?)(await provider.QueryAsync(query));
        }
        async Task<bool> IRepo.QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (bool)(await provider.QueryAsync(query))!;
        }
        async Task<long> IRepo.QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (long)(await provider.QueryAsync(query))!;
        }
        async Task<IReadOnlyCollection<EventModel<TModel>>> IRepo.QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return (IReadOnlyCollection<EventModel<TModel>>)(await provider.QueryAsync(query))!;
        }

        Task IRepo.PersistAsync<TModel>(Persist<TModel> persist) where TModel : class, new()
        {
            if (!providers.TryGetValue(typeof(TModel), out var providerObj))
                throw new Exception($"No provider for {typeof(TModel).FullName}");
            var provider = (ITransactStoreProvider<TModel>)providerObj;
            return provider.PersistAsync(persist);
        }

        void IRepoSetup.AddProvider<TModel>(ITransactStoreProvider<TModel> provider) where TModel : class, new()
        {
            if (!providers.TryAdd(typeof(TModel), provider))
                throw new Exception($"Provider for {typeof(TModel).FullName} already exists");
        }
    }
}
