using Zerra.Repository;

namespace Store.Shipping.Service.Data
{
    public sealed class ShippingStoreProvider<TModel> : TransactStoreProvider<ShippingDataContext, TModel>
        where TModel : class, new()
    {
        public ShippingStoreProvider() { }
        //a context of its own is a store of its own when it's in-memory, the tests give each test one
        public ShippingStoreProvider(ShippingDataContext context) : base(context) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
