// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Repository
{
    public abstract class DataContextSelector : DataContext
    {
        protected override sealed bool DisableBuildStoreFromModels => true;

        protected override T GetEngine<T>()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                if (!context.TryGetEngine(out T engine))
                    continue;
                return engine;
            }
            return null;
        }
        protected override sealed IDataStoreEngine GetEngine()
        {
            throw new NotSupportedException();
        }

        protected abstract ICollection<DataContext> GetDataContexts();
    }
}
