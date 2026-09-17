using Store.Common;
using Zerra.Repository;
using Zerra.Repository.KurrentDB;
using Zerra.Repository.Memory;

namespace Store.Carts.Service.Data
{
    /// <summary>
    /// The Carts service's own event store: KurrentDB when it's reachable, otherwise in-memory. Both engines are event stores, which the cart aggregate needs.
    /// </summary>
    public sealed class CartsDataContext : DataContextSelector
    {
        protected override IEnumerable<DataContext> LoadDataContexts() => StoreSettings.InMemoryOnly
            ? [new CartsMemoryContext()]
            : [new CartsKurrentDbContext(), new CartsMemoryContext()];
    }

    public sealed class CartsKurrentDbContext : KurrentDbDataContext
    {
        public override string ConnectionString => StoreSettings.CartsKurrentDb;
        public override bool Insecure => true;
    }

    public sealed class CartsMemoryContext : MemoryDataContext
    {
    }
}
