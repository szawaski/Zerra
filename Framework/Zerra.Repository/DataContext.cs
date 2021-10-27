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
        private static bool isValid = false;
        private static bool validated = false;
        private static readonly object validatedLock = new object();

        public bool TryGetEngine<T>(out T engine) where T : class, IDataStoreEngine
        {
            var dataStoreEngine = GetEngine();

            if (!(dataStoreEngine is T casted))
            {
                engine = null;
                return false;
            }

            lock (validatedLock)
            {
                if (!validated)
                {
                    validated = true;
                    isValid = dataStoreEngine.ValidateDataSource();
                }

                if (!isValid)
                {
                    engine = null;
                    return false;
                }
            }

            engine = casted;
            return true;
        }

        private static bool initialized = false;
        private static readonly object initializedLock = new object();
        public T InitializeEngine<T>() where T : class, IDataStoreEngine
        {
            var engine = GetEngine();

            if (!(engine is T casted))
                throw new Exception($"{this.GetType().Name} does not produce {typeof(T)}");

            lock (validatedLock)
            {
                if (!validated)
                {
                    validated = true;
                    isValid = engine.ValidateDataSource();
                }
                if (!isValid)
                    throw new Exception($"{this.GetType().Name} failed to validate data source");
            }

            lock (initializedLock)
            {
                if (!initialized)
                {
                    initialized = true;
                    if (!DisableBuildStoreFromModels)
                    {
                        var modelTypes = Discovery.GetTypesFromAttribute(typeof(DataSourceEntityAttribute));
                        var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
                        engine.BuildStoreFromModels(modelDetails);
                    }
                }
            }

            return casted;
        }

        private static IDataStoreEngine engineCache = null;
        private static readonly object engineCacheLock = new object();
        private IDataStoreEngine GetEngine()
        {
            if (engineCache == null)
            {
                lock (engineCacheLock)
                {
                    if (engineCache == null)
                    {
                        engineCache = GetDataStoreEngine();
                    }
                }
            }
            return engineCache;
        }

        protected abstract IDataStoreEngine GetDataStoreEngine();

        protected abstract bool DisableBuildStoreFromModels { get; }
    }
}
