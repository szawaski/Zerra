// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;

namespace Zerra.Repository
{
    public abstract class DataContextSelector : DataContext
    {
        protected override sealed bool DisableBuildStoreFromModels => false;

        protected override sealed (T, bool) GetEngine<T>()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                if (!context.TryGetEngine(out T engine, out bool disableBuildStore))
                    continue;
                return (engine, disableBuildStore);
            }
            return (null, default);
        }
        protected override sealed IDataStoreEngine GetEngine()
        {
            throw new NotSupportedException();
        }

        protected abstract ICollection<DataContext> GetDataContexts();
    }
}
