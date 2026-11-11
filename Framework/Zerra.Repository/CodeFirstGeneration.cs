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
        /// <param name="engine">The engine of the data store to generate.</param>
        /// <param name="mode">The changes allowed, at least one of <see cref="CodeFirstMode.CreateDataStore"/>, <see cref="CodeFirstMode.Columns"/>, <see cref="CodeFirstMode.Constraints"/>, or <see cref="CodeFirstMode.DropColumns"/>, with <see cref="CodeFirstMode.Preview"/> to only log the plan.</param>
        /// <param name="modelTypes">The model types to analyze and use for schema generation.</param>
        /// <param name="log">An optional logger for reporting generation progress and plan previews.</param>
        public static void Generate(ITransactStoreEngine engine, CodeFirstMode mode, Type[] modelTypes, ILogger? log = null)
        {
            if (engine is null)
                throw new ArgumentNullException(nameof(engine));

            var createDataStore = mode.HasFlag(CodeFirstMode.CreateDataStore);
            var columns = mode.HasFlag(CodeFirstMode.Columns);
            var constraints = mode.HasFlag(CodeFirstMode.Constraints);
            var dropColumns = mode.HasFlag(CodeFirstMode.DropColumns);
            if (!createDataStore && !columns && !constraints && !dropColumns)
                throw new ArgumentException($"{nameof(CodeFirstMode)} needs at least one of {nameof(CodeFirstMode.CreateDataStore)}, {nameof(CodeFirstMode.Columns)}, {nameof(CodeFirstMode.Constraints)}, or {nameof(CodeFirstMode.DropColumns)}", nameof(mode));

            log?.Info($"{engine.GetType().Name} Initializing {mode.EnumName()}");

            var modelDetails = modelTypes.Select(x => ModelAnalyzer.GetModel(x)).ToArray();
            foreach (var modelDetail in modelDetails)
            {
                if (!modelDetail.IsDataSourceEntity)
                    throw new Exception($"{modelDetail.Type.Name} does not have an EntityAttribute");
            }

            IDataStoreGenerationPlan plan;
            try
            {
                plan = engine.BuildStoreGenerationPlan(createDataStore, columns, constraints, dropColumns, modelDetails);
            }
            catch (Exception ex)
            {
                log?.Error($"{engine.GetType().Name} error while reading datastore.", ex);
                throw;
            }

            if (mode.HasFlag(CodeFirstMode.Preview))
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
