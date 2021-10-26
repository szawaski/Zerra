// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Repository
{
    public abstract class DataContextSelector : DataContext
    {
        protected override sealed bool DisableAssureDataStore => true;
        protected override IDataStoreEngine GetDataStoreEngine()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                if (!context.TryGetEngine(out IDataStoreEngine engine))
                    continue;
                return engine;
            }
            throw new Exception($"{nameof(DataContextSelector)} did not find an appropriate {nameof(IDataStoreEngine)}");
        }

        protected abstract ICollection<DataContext> GetDataContexts();
    }
}
