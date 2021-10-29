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
        public static ICollection<TModel> Query<TModel>(QueryMany<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (ICollection<TModel>)(provider.Query(query));
        }
        public static TModel Query<TModel>(QueryFirst<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (TModel)(provider.Query(query));
        }
        public static TModel Query<TModel>(QuerySingle<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (TModel)(provider.Query(query));
        }
        public static bool Query<TModel>(QueryAny<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (bool)(provider.Query(query));
        }
        public static long Query<TModel>(QueryCount<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (long)(provider.Query(query));
        }
        public static ICollection<EventModel<TModel>> Query<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (ICollection<EventModel<TModel>>)(provider.Query(query));
        }

        public static void Persist<TModel>(Persist<TModel> persist) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            provider.Persist(persist);
        }

        public static async Task<ICollection<TModel>> QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (ICollection<TModel>)(await provider.QueryAsync(query));
        }
        public static async Task<TModel> QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (TModel)(await provider.QueryAsync(query));
        }
        public static async Task<TModel> QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (TModel)(await provider.QueryAsync(query));
        }
        public static async Task<bool> QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (bool)(await provider.QueryAsync(query));
        }
        public static async Task<long> QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (long)(await provider.QueryAsync(query));
        }
        public static async Task<ICollection<EventModel<TModel>>> QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return (ICollection<EventModel<TModel>>)(await provider.QueryAsync(query));
        }

        public static Task PersistAsync<TModel>(Persist<TModel> persist) where TModel : class, new()
        {
            var provider = Resolver.Get<ITransactStoreProvider<TModel>>();
            return provider.PersistAsync(persist);
        }
    }
}
