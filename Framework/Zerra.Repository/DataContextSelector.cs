// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Repository
{
    public abstract class DataContextSelector<T> : DataContext<T> where T : class, IDataStoreEngine
    {
        protected override sealed bool DisableBuildStoreFromModels => true;

        protected override T GetEngine()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                if (!context.TryGetEngine(out T engine))
                    continue;
                return engine;
            }
            throw new Exception($"{nameof(DataContextSelector<T>)} did not find a {nameof(T)}");
        }

        protected abstract ICollection<DataContext<T>> GetDataContexts();
    }
}
