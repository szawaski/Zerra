using Store.Common;
using Zerra.Repository;
using Zerra.Repository.Memory;
using Zerra.Repository.MySql;

namespace Store.Inventory.Service.Data
{
    /// <summary>
    /// The Inventory service's own data store: MySQL when it's reachable, otherwise in-memory.
    /// </summary>
    public sealed class InventoryDataContext : DataContextSelector
    {
        protected override IEnumerable<DataContext> LoadDataContexts() => StoreSettings.InMemoryOnly
            ? [new InventoryMemoryContext()]
            : [new InventoryMySqlContext(), new InventoryMemoryContext()];
    }

    public sealed class InventoryMySqlContext : MySqlDataContext
    {
        public override string GetConnectionString() => StoreSettings.InventoryMySql;
    }

    public sealed class InventoryMemoryContext : MemoryDataContext
    {
    }
}
