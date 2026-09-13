using Store.Common;
using Zerra.Repository;
using Zerra.Repository.Memory;
using Zerra.Repository.MsSql;

namespace Store.Orders.Service.Data
{
    /// <summary>
    /// The Orders service's own data store: SQL Server when it's reachable, otherwise in-memory.
    /// </summary>
    public sealed class OrdersDataContext : DataContextSelector
    {
        protected override IEnumerable<DataContext> LoadDataContexts() => StoreSettings.InMemoryOnly
            ? [new OrdersMemoryContext()]
            : [new OrdersMsSqlContext(), new OrdersMemoryContext()];
    }

    public sealed class OrdersMsSqlContext : MsSqlDataContext
    {
        public override string GetConnectionString() => StoreSettings.OrdersMsSql;
    }

    public sealed class OrdersMemoryContext : MemoryDataContext
    {
    }
}
