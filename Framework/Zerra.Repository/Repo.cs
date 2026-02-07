// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.Providers;

namespace Zerra.Repository
{
    public static class Repo
    {
        public static IReadOnlyCollection<TModel> Query<TModel>(QueryMany<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (IReadOnlyCollection<TModel>)(provider.Query(query))!;
        }
        public static TModel? Query<TModel>(QueryFirst<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (TModel?)(provider.Query(query));
        }
        public static TModel? Query<TModel>(QuerySingle<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (TModel?)(provider.Query(query));
        }
        public static bool Query<TModel>(QueryAny<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (bool)(provider.Query(query))!;
        }
        public static long Query<TModel>(QueryCount<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (long)(provider.Query(query))!;
        }
        public static IReadOnlyCollection<EventModel<TModel>> Query<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (IReadOnlyCollection<EventModel<TModel>>)(provider.Query(query))!;
        }

        public static void Persist<TModel>(Persist<TModel> persist) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            provider.Persist(persist);
        }

        public static async Task<IReadOnlyCollection<TModel>> QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (IReadOnlyCollection<TModel>)(await provider.QueryAsync(query))!;
        }
        public static async Task<TModel?> QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (TModel?)(await provider.QueryAsync(query));
        }
        public static async Task<TModel?> QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (TModel?)(await provider.QueryAsync(query));
        }
        public static async Task<bool> QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (bool)(await provider.QueryAsync(query))!;
        }
        public static async Task<long> QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (long)(await provider.QueryAsync(query))!;
        }
        public static async Task<IReadOnlyCollection<EventModel<TModel>>> QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return (IReadOnlyCollection<EventModel<TModel>>)(await provider.QueryAsync(query))!;
        }

        public static Task PersistAsync<TModel>(Persist<TModel> persist) where TModel : class, new()
        {
            var provider = ProviderResolver.GetFirst<ITransactStoreProvider<TModel>>();
            return provider.PersistAsync(persist);
        }
    }
}
