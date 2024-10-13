// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Logging;

namespace Zerra.Repository
{
    public abstract class DataContextSelector : DataContext
    {
        protected sealed override DataStoreGenerationType DataStoreGenerationType => base.DataStoreGenerationType; //does nothing

        protected override sealed (T?, DataStoreGenerationType) GetEngine<T>() where T : class
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                var contextName = context.GetType().GetNiceName();
                Log.InfoAsync($"{nameof(DataContextSelector)} trying {contextName}");
                if (!context.TryGetEngine<T>(out var engine, out var dataStoreGenerationType))
                {
                    Log.InfoAsync($"{nameof(DataContextSelector)} could not validate {contextName}");
                    continue;
                }
                Log.InfoAsync($"{nameof(DataContextSelector)} found {contextName}");
                return (engine, dataStoreGenerationType);
            }
            return (null, default);
        }
        
        protected override sealed IDataStoreEngine GetEngine()
        {
            throw new NotSupportedException();
        }

        private readonly object locker = new();
        private ICollection<DataContext>? contexts = null;
        protected ICollection<DataContext> GetDataContexts()
        {
            if (contexts is null)
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
