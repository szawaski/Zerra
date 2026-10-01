using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    public sealed class InventoryStoreProvider<TModel> : TransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public InventoryStoreProvider(ITransactStoreEngine engine) : base(engine) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
