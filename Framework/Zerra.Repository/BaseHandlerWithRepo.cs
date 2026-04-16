using Zerra.CQRS;

namespace Zerra.Repository
{
    public abstract class BaseHandlerWithRepo : BaseHandler
    {
        private IRepo? repo;
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
