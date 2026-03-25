// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public interface IRepoSetup : IRepo
    {
        void AddProvider<TModel>(ITransactStoreProvider<TModel> provider) where TModel : class, new();
    }
}
