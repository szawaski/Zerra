// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    public abstract class DataContext<T> : DataContext where T : class, IDataStoreEngine
    {
        protected override sealed IDataStoreEngine GetDataStoreEngine()
        {
            return GetEngine();
        }

        protected abstract T GetEngine();
    }
}
