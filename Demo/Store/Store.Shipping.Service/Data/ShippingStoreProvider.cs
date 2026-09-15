using Zerra.Repository;

namespace Store.Shipping.Service.Data
{
    public sealed class ShippingStoreProvider<TModel> : TransactStoreProvider<ShippingDataContext, TModel>
        where TModel : class, new()
    {
        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
