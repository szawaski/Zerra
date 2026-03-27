// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public sealed partial class Repo : IRepoSetup
    {
        private static IRepoSetup? staticRepo;
        private static IRepoSetup StaticRepo => staticRepo ?? throw new InvalidOperationException("Repo not initialized. Call Repo.New to initialize.");

        public static IReadOnlyCollection<TModel> Query<TModel>(QueryMany<TModel> query) where TModel : class, new()
            => StaticRepo.Query(query);
        public static TModel? Query<TModel>(QueryFirst<TModel> query) where TModel : class, new()
            => StaticRepo.Query(query);
        public static TModel? Query<TModel>(QuerySingle<TModel> query) where TModel : class, new()
            => StaticRepo.Query(query);
        public static bool Query<TModel>(QueryAny<TModel> query) where TModel : class, new()
            => StaticRepo.Query(query);
        public static long Query<TModel>(QueryCount<TModel> query) where TModel : class, new()
            => StaticRepo.Query(query);
        public static IReadOnlyCollection<EventModel<TModel>> Query<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
            => StaticRepo.Query(query);

        public static void Persist(Persist persist)
            => StaticRepo.Persist(persist);

        public static Task<IReadOnlyCollection<TModel>> QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class, new()
            => StaticRepo.QueryAsync(query);
        public static Task<TModel?> QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class, new()
             => StaticRepo.QueryAsync(query);
        public static Task<TModel?> QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class, new()
             => StaticRepo.QueryAsync(query);
        public static Task<bool> QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class, new()
             => StaticRepo.QueryAsync(query);
        public static Task<long> QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class, new()
             => StaticRepo.QueryAsync(query);
        public static Task<IReadOnlyCollection<EventModel<TModel>>> QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class, new()
             => StaticRepo.QueryAsync(query);

        public static Task PersistAsync(Persist persist)
            => StaticRepo.PersistAsync(persist);
    }
}
