// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public interface ITransactStoreProvider<TModel> : ITransactStoreProvider where TModel : class, new()
    {
    }
    public interface ITransactStoreProvider
    {
        Type ModelType { get; }

        object? Query(Query query);
        Task<object?> QueryAsync(Query query);

        void Persist(Persist persist);
        Task PersistAsync(Persist persist);
    }
}
