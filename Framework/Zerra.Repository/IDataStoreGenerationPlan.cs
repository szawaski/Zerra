using Zerra.Logging;

namespace Zerra.Repository
{
    /// <summary>
    /// Represents a plan for generating or modifying a data store schema.
    /// </summary>
    public interface IDataStoreGenerationPlan
    {
        /// <summary>
        /// Gets the collection of statements or commands that make up the generation plan.
        /// </summary>
        ICollection<string> Plan { get; }

        /// <summary>
        /// Executes the generation plan against the data store.
        /// </summary>
        void Execute(ILogger? log);
    }
}
