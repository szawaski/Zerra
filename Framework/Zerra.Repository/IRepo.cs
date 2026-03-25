// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public interface IRepo
    {
        IReadOnlyCollection<TModel> Query<TModel>(QueryMany<TModel> query) where TModel : class, new();
        TModel? Query<TModel>(QueryFirst<TModel> query) where TModel : class, new();
        TModel? Query<TModel>(QuerySingle<TModel> query) where TModel : class, new();
        bool Query<TModel>(QueryAny<TModel> query) where TModel : class, new();
        long Query<TModel>(QueryCount<TModel> query) where TModel : class, new();
        IReadOnlyCollection<EventModel<TModel>> Query<TModel>(EventQueryMany<TModel> query) where TModel : class, new();
        void Persist<TModel>(Persist<TModel> persist) where TModel : class, new();
        Task<IReadOnlyCollection<TModel>> QueryAsync<TModel>(QueryMany<TModel> query) where TModel : class, new();
        Task<TModel?> QueryAsync<TModel>(QueryFirst<TModel> query) where TModel : class, new();
        Task<TModel?> QueryAsync<TModel>(QuerySingle<TModel> query) where TModel : class, new();
        Task<bool> QueryAsync<TModel>(QueryAny<TModel> query) where TModel : class, new();
        Task<long> QueryAsync<TModel>(QueryCount<TModel> query) where TModel : class, new();
        Task<IReadOnlyCollection<EventModel<TModel>>> QueryAsync<TModel>(EventQueryMany<TModel> query) where TModel : class, new();
        Task PersistAsync<TModel>(Persist<TModel> persist) where TModel : class, new();
    }
}
