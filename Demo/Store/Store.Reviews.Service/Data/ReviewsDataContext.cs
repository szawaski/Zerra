using Store.Common;
using Zerra.Repository;
using Zerra.Repository.MariaDb;
using Zerra.Repository.Memory;

namespace Store.Reviews.Service.Data
{
    /// <summary>
    /// The Reviews service's own data store: MariaDB when it's reachable, otherwise in-memory.
    /// </summary>
    public sealed class ReviewsDataContext : DataContextSelector
    {
        protected override IEnumerable<DataContext> LoadDataContexts() => StoreSettings.InMemoryOnly
            ? [new ReviewsMemoryContext()]
            : [new ReviewsMariaDbContext(), new ReviewsMemoryContext()];
    }

    public sealed class ReviewsMariaDbContext : MariaDbDataContext
    {
        public override string GetConnectionString() => StoreSettings.ReviewsMariaDb;
    }

    public sealed class ReviewsMemoryContext : MemoryDataContext
    {
    }
}
