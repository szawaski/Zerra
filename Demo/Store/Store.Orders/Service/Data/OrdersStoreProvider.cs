using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    public sealed class OrdersStoreProvider<TModel> : TransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public OrdersStoreProvider(ITransactStoreEngine engine) : base(engine) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
