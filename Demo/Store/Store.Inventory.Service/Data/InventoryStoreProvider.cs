using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    public sealed class InventoryStoreProvider<TModel> : TransactStoreProvider<InventoryDataContext, TModel>
        where TModel : class, new()
    {
        public InventoryStoreProvider() { }
        //a context of its own is a store of its own when it's in-memory, the tests give each test one
        public InventoryStoreProvider(InventoryDataContext context) : base(context) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
