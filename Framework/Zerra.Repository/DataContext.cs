// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Zerra.Logging;
using Zerra.Reflection;
using Zerra.Reflection.Dynamic;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public abstract class DataContext
    {
        private static bool isValid = false;
        private static bool validated = false;
        private static readonly object validatedLock = new();

        public bool TryGetEngine<T>(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out T engine, out DataStoreGenerationType dataStoreGenerationType) where T : class, IDataStoreEngine
        {
            (engine, dataStoreGenerationType) = GetEngine<T>();
            if (engine is null)
                return false;

            lock (validatedLock)
            {
                if (!validated)
                {
                    validated = true;
                    isValid = engine.ValidateDataSource();
                    if (isValid)
                        Log.InfoAsync($"{this.GetType().Name} connected");
                    else
                        Log.InfoAsync($"{this.GetType().Name} failed to connect");
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
        private static readonly object initializedLock = new();
        public T InitializeEngine<T>(bool reinitialize = false) where T : class, IDataStoreEngine
        {
            var (engine, dataStoreGenerationType) = GetEngine<T>();
            if (engine is null)
                throw new Exception($"{this.GetType().Name} could not produce an engine of {typeof(T).Name}");

            lock (validatedLock)
            {
                if (!validated || reinitialize)
                {
                    validated = true;
                    isValid = engine.ValidateDataSource();
                    if (isValid)
                        Log.InfoAsync($"{this.GetType().Name} connected");
                    else
                        Log.InfoAsync($"{this.GetType().Name} failed to connect");
                }
                if (!isValid)
                    throw new Exception($"{this.GetType().Name} could not validate");
            }

            lock (initializedLock)
            {
                if (!initialized || reinitialize)
                {
                    initialized = true;

                    if (dataStoreGenerationType.HasFlag(DataStoreGenerationType.CodeFirst))
                    {
                        Log.InfoAsync($"{this.GetType().Name} Initializing {dataStoreGenerationType.EnumName()}");

                        var thisType = this.GetType();
                        var allModelTypes = Discovery.GetTypesFromAttribute(typeof(EntityAttribute));
                        var modelTypesWithThisDataContext = new HashSet<Type>();
                        foreach (var modelType in allModelTypes.Where(x => !x.IsAbstract))
                        {
                            var interfaceType = typeof(ITransactStoreProvider<>).MakeGenericType(modelType);
                            var providerType = Discovery.GetClassByInterface(interfaceType);
                            if (providerType is null)
                                continue;
                            var typeDetails = TypeAnalyzer.GetTypeDetail(providerType);
                            if (typeDetails.InnerTypes.Contains(thisType))
                            {
                                _ = modelTypesWithThisDataContext.Add(modelType);
                                continue;
                            }
                            foreach (var baseType in typeDetails.BaseTypes)
                            {
                                var baseTypeDetails = TypeAnalyzer.GetTypeDetail(baseType);
                                if (baseTypeDetails.InnerTypes.Contains(thisType))
                                {
                                    _ = modelTypesWithThisDataContext.Add(modelType);
                                    continue;
                                }
                            }
                        }

                        var modelDetails = modelTypesWithThisDataContext.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
                        var create = !dataStoreGenerationType.HasFlag(DataStoreGenerationType.NoCreate);
                        var update = !dataStoreGenerationType.HasFlag(DataStoreGenerationType.NoUpdate);
                        var delete = !dataStoreGenerationType.HasFlag(DataStoreGenerationType.NoDelete);

                        var plan = engine.BuildStoreGenerationPlan(create, update, delete, modelDetails);

                        if (dataStoreGenerationType.HasFlag(DataStoreGenerationType.Preview))
                        {
                            var sb = new StringBuilder();
                            var steps = plan.Plan;
                            _ = sb.AppendLine($"CodeFirst Plan Preview: {steps.Count} Steps");
                            foreach (var step in steps)
                                _ = sb.AppendLine(step);
                            _ = Log.InfoAsync(sb.ToString());
                        }
                        else
                        {
                            plan.Execute();
                        }
                    }
                }
            }

            return engine;
        }

        protected virtual (T?, DataStoreGenerationType) GetEngine<T>() where T : class, IDataStoreEngine
        {
            return (GetEngine() as T, DataStoreGenerationType);
        }
        protected abstract IDataStoreEngine GetEngine();
        protected abstract DataStoreGenerationType DataStoreGenerationType { get; }
    }
}
