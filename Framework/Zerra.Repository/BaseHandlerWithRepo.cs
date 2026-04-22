using Zerra.CQRS;

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for command, event, and query handlers that require access to a repository.
    /// Extends <see cref="BaseHandler"/> with a lazily resolved <see cref="IRepo"/> instance.
    /// </summary>
    public abstract class BaseHandlerWithRepo : BaseHandler
    {
        private IRepo repo = null!;
        /// <summary>
        /// Gets the repository instance resolved from the current bus context.
        /// </summary>
        public IRepo Repo => repo;

        /// <inheritdoc />
        protected override sealed void InitializeBaseHandler()
        {
            var getRepo = Context.GetService<IRepo>();
            if (getRepo == null)
                throw new InvalidOperationException("Failed to resolve IRepo from the current bus context.");
            repo = getRepo;
            InitializeBaseHandlerWithRepo();
        }

        /// <summary>
        /// Called after the handler is initialized with the repository and the bus context.
        /// Override this method to perform custom initialization logic.
        /// </summary>
        protected virtual void InitializeBaseHandlerWithRepo() { }
    }
}
