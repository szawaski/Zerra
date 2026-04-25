// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Zerra.Logging;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Provides functionality for generating and updating data store schemas based on code-first model definitions.
    /// </summary>
    public static class CodeFirstGeneration
    {
        /// <summary>
        /// Generates or updates the data store schema based on the provided model types and generation options.
        /// </summary>
        /// <typeparam name="TContext">The data context type that provides the data store engine.</typeparam>
        /// <param name="dataStoreGenerationType">Flags that control how schema generation is performed, including options to preview, restrict creates, updates, or deletes.</param>
        /// <param name="modelTypes">The model types to analyze and use for schema generation.</param>
        /// <param name="log">An optional logger for reporting generation progress and plan previews.</param>
        public static void Generate<TContext>(DataStoreGenerationType dataStoreGenerationType, Type[] modelTypes, ILogger? log = null)
            where TContext : DataContext, new()
        {
            if (dataStoreGenerationType.HasFlag(DataStoreGenerationType.CodeFirst))
            {
                var context = new TContext();
                if (!context.TryGetEngine(out var engine))
                    throw new Exception($"DataContext {typeof(TContext).FullName} did not return an engine");

                log?.Info($"{engine.GetType().Name} Initializing {dataStoreGenerationType.EnumName()}");

                var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
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
                    log?.Info(sb.ToString());
                }
                else
                {
                    plan.Execute(log);
                }
            }
        }
    }
}
