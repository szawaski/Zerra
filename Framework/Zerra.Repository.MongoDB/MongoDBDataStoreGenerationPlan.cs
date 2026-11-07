// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Logging;

namespace Zerra.Repository.MongoDB
{
    /// <summary>
    /// The collections and indexes to create, change, or drop to match the models. A document has no schema, so its fields aren't planned.
    /// </summary>
    public sealed class MongoDBDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        internal readonly struct Step
        {
            public readonly string Description;
            public readonly Action Action;
            public Step(string description, Action action)
            {
                this.Description = description;
                this.Action = action;
            }
        }

        private readonly List<Step> steps;

        internal MongoDBDataStoreGenerationPlan(List<Step> steps)
        {
            this.steps = steps;
        }

        /// <summary>
        /// Gets each change, in the order it is made.
        /// </summary>
        public ICollection<string> Plan
        {
            get
            {
                var plan = new string[steps.Count];
                for (var i = 0; i < plan.Length; i++)
                    plan[i] = steps[i].Description;
                return plan;
            }
        }

        /// <summary>
        /// Makes the changes in order.
        /// </summary>
        /// <param name="log">Optional logger, told of each change and of an error.</param>
        public void Execute(ILogger? log)
        {
            foreach (var step in steps)
            {
                try
                {
                    step.Action();
                    log?.Info($"{nameof(MongoDBEngine)}: {step.Description}");
                }
                catch (Exception ex)
                {
                    log?.Error($"{nameof(MongoDBEngine)} error while assuring datastore: {step.Description}.", ex);
                    throw;
                }
            }
        }
    }
}
