using Zerra.CQRS;

namespace Zerra.Repository
{
    /// <summary>
    /// Base class for command, event, and query handlers that require access to a repository.
    /// Extends <see cref="BaseHandler"/> with a lazily resolved <see cref="IRepo"/> instance.
    /// </summary>
    public abstract class BaseHandlerWithRepo : BaseHandler
    {
        private IRepo? repo;
        /// <summary>
        /// Gets the repository instance resolved from the current bus context.
        /// </summary>
        public IRepo Repo
        {
            get
            {
                repo ??= Context.GetService<IRepo>();
                return repo;
            }
        }
    }
}
