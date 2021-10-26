// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class DataContext
    {
        public bool TryGetEngine<T>(out T engine) where T : class, IDataStoreEngine
        {
            var dataStoreEngine = GetDataStoreEngine();

            if (!(dataStoreEngine is T casted))
            {
                engine = null;
                return false;
            }

            if (!dataStoreEngine.DetectIsDataSource())
            {
                engine = null;
                return false;
            }

            engine = casted;
            return true;
        }

        public T InitializeEngine<T>() where T : class, IDataStoreEngine
        {
            var engine = GetDataStoreEngine();

            if (!(engine is T casted))
                throw new Exception($"{this.GetType().Name} does not produce {typeof(T)}");

            if (!engine.DetectIsDataSource())
                throw new Exception($"{this.GetType().Name} failed to connect");

            if (!DisableAssureDataStore)
            {
                var modelTypes = Discovery.GetTypesFromAttribute(typeof(DataSourceEntityAttribute));
                var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
                engine.AssureDataStore(modelDetails);
            }

            return casted;
        }

        protected abstract IDataStoreEngine GetDataStoreEngine();

        protected abstract bool DisableAssureDataStore { get; }
    }
}
