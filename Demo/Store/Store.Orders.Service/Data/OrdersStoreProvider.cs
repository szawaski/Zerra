using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    public sealed class OrdersStoreProvider<TModel> : TransactStoreProvider<OrdersDataContext, TModel>
        where TModel : class, new()
    {
        public OrdersStoreProvider() { }
        //a context of its own is a store of its own when it's in-memory, the tests give each test one
        public OrdersStoreProvider(OrdersDataContext context) : base(context) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
