// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Threading.Tasks;

namespace Zerra.Repository
{
    public interface ITransactStoreProvider<TModel> where TModel : class, new()
    {
        object? Query(Query<TModel> query);
        Task<object?> QueryAsync(Query<TModel> query);

        void Persist(Persist<TModel> persist);
        Task PersistAsync(Persist<TModel> persist);
    }
}
