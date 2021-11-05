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
        private bool disableBuildStoreFromModels = false;
        protected override sealed bool DisableBuildStoreFromModels => disableBuildStoreFromModels;

        protected override T GetEngine<T>()
        {
            var contexts = GetDataContexts();
            foreach (var context in contexts)
            {
                if (!context.TryGetEngine(out T engine))
                    continue;
                disableBuildStoreFromModels = (bool)TypeAnalyzer.GetTypeDetail(typeof(DataContext)).GetMember(nameof(DisableBuildStoreFromModels)).Getter(context);
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
