using Zerra.Repository;

namespace Store.Inventory.Service.Data
{
    public sealed class InventoryStoreProvider<TModel> : TransactStoreProvider<InventoryDataContext, TModel>
        where TModel : class, new()
    {
        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
