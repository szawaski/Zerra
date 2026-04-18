// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Zerra.Logging;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public static class CodeFirstGeneration
    {
        public static void Generate(IDataStoreEngine engine, DataStoreGenerationType dataStoreGenerationType, Type[] modelTypes, ILogger? log = null)
        {
            if (dataStoreGenerationType.HasFlag(DataStoreGenerationType.CodeFirst))
            {
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
                    plan.Execute();
                }
            }
        }
    }
}
