using Store.Common;
using Zerra.Repository;
using Zerra.Repository.Memory;
using Zerra.Repository.PostgreSql;

namespace Store.Catalog.Service.Data
{
    /// <summary>
    /// The Catalog service's own data store: PostgreSQL when it's reachable, otherwise in-memory.
    /// </summary>
    public sealed class CatalogDataContext : DataContextSelector
    {
        protected override IEnumerable<DataContext> LoadDataContexts() => StoreSettings.InMemoryOnly
            ? [new CatalogMemoryContext()]
            : [new CatalogPostgreSqlContext(), new CatalogMemoryContext()];
    }

    public sealed class CatalogPostgreSqlContext : PostgreSqlDataContext
    {
        public override string GetConnectionString() => StoreSettings.CatalogPostgreSql;
    }

    public sealed class CatalogMemoryContext : MemoryDataContext
    {
    }
}
