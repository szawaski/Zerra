using Zerra.Repository;

namespace Store.Shipping.Service.Data
{
    public sealed class ShippingStoreProvider<TModel> : TransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public ShippingStoreProvider(ITransactStoreEngine engine) : base(engine) { }

        protected override bool EventLinking => false;
    }
}
