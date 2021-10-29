// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using Zerra.Providers;
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
            engine = GetEngine<T>();
            if (engine == null)
                return false;

            lock (validatedLock)
            {
                if (!validated)
                {
                    validated = true;
                    isValid = engine.ValidateDataSource();
                }

                if (!isValid)
                {
                    engine = null;
                    return false;
                }
            }

            return true;
        }

        private static bool initialized = false;
        private static readonly object initializedLock = new object();
        public T InitializeEngine<T>(bool reinitialize = false) where T : class, IDataStoreEngine
        {
            var engine = GetEngine<T>();
            if (engine == null)
                throw new Exception($"{this.GetType().Name} could not produce an engine of {typeof(T).Name}");

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
                        var thisType = this.GetType();
                        var allModelTypes = Discovery.GetTypesFromAttribute(typeof(EntityAttribute));
                        var modelTypesWithThisDataContext = new HashSet<Type>();
                        foreach(var modelType in allModelTypes.Where(x => !x.IsAbstract))
                        {
                            var providerType = typeof(ITransactStoreProvider<>).MakeGenericType(modelType);
                            if (!Resolver.TryGet(providerType, out object provider))
                                continue;
                            var typeDetails = TypeAnalyzer.GetType(provider.GetType());
                            if (typeDetails.InnerTypes.Contains(thisType))
                            {
                                modelTypesWithThisDataContext.Add(modelType);
                                continue;
                            }
                            foreach(var baseType in typeDetails.BaseTypes)
                            {
                                var baseTypeDetails = TypeAnalyzer.GetType(baseType);
                                if (baseTypeDetails.InnerTypes.Contains(thisType))
                                {
                                    modelTypesWithThisDataContext.Add(modelType);
                                    continue;
                                }
                            }
                        }

                        var modelDetails = modelTypesWithThisDataContext.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
                        engine.BuildStoreFromModels(modelDetails);
                    }
                }
            }

            return engine;
        }

        protected virtual T GetEngine<T>() where T : class, IDataStoreEngine
        {
            return GetEngine() as T;
        }
        protected abstract IDataStoreEngine GetEngine();
        protected virtual bool DisableBuildStoreFromModels => false;
    }
}
