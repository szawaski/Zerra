using Zerra.Logging;

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a no-op data store generation plan that contains no changes and performs no actions.
    /// </summary>
    public sealed class EmptyDataStoreGenerationPlan : IDataStoreGenerationPlan
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
