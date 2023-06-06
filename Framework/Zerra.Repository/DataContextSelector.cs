// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Repository
{
    public abstract class DataContextSelector : DataContext
    {
        protected sealed override DataStoreGenerationType DataStoreGenerationType => base.DataStoreGenerationType; //does nothing

        protected override sealed (T, DataStoreGenerationType) GetEngine<T>()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                if (!context.TryGetEngine(out T engine, out var dataStoreGenerationType))
                    continue;
                return (engine, dataStoreGenerationType);
            }
            return (null, default);
        }
        protected override sealed IDataStoreEngine GetEngine()
        {
            throw new NotSupportedException();
        }

        private readonly object locker = new();
        private ICollection<DataContext> contexts = null;
        protected ICollection<DataContext> GetDataContexts()
        {
            if (contexts == null)
            {
                lock (locker)
                {
                    contexts ??= LoadDataContexts();
                }
            }
            return contexts;
        }
        protected abstract ICollection<DataContext> LoadDataContexts();
    }
}
