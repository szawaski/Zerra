using Zerra.Logging;

namespace Zerra.Repository.Memory
{
    /// <summary>
    /// The generation plan of an in-memory store, which has no schema, so it contains no changes and performs no actions.
    /// </summary>
    public sealed class MemoryDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        /// <summary>
        /// Gets an empty collection indicating no schema changes are planned.
        /// </summary>
        public ICollection<string> Plan => Array.Empty<string>();

        /// <summary>
        /// Performs no action as there are no schema changes to execute.
        /// </summary>
        public void Execute(ILogger? log) { }
    }
}
