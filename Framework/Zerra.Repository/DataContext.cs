// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using Zerra.Reflection;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class DataContext<T> where T : class, IDataStoreEngine
    {
        private static bool isValid = false;
        private static bool validated = false;
        private static readonly object validatedLock = new object();

        public bool TryGetEngine(out T engine)
        {
            var dataStoreEngine = GetEngine();

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

            engine = dataStoreEngine;
            return true;
        }

        private static bool initialized = false;
        private static readonly object initializedLock = new object();
        public T InitializeEngine(bool reinitialize = false)
        {
            var engine = GetEngine();

            lock (validatedLock)
            {
                if (!validated || reinitialize)
                {
                    validated = true;
                    isValid = engine.ValidateDataSource();
                }
                if (!isValid)
                    throw new Exception($"{this.GetType().Name} failed to validate data source");
            }

            lock (initializedLock)
            {
                if (!initialized || reinitialize)
                {
                    initialized = true;
                    if (!DisableBuildStoreFromModels)
                    {
                        var modelTypes = Discovery.GetTypesFromAttribute(typeof(DataSourceEntityAttribute));
                        var modelDetails = modelTypes.Where(x => !x.IsAbstract).Select(x => ModelAnalyzer.GetModel(x)).Distinct(x => x.DataSourceEntityName).ToArray();
                        engine.BuildStoreFromModels(modelDetails);
                    }
                }
            }

            return engine;
        }

        protected abstract T GetEngine();
        protected virtual bool DisableBuildStoreFromModels => false;
    }
}
