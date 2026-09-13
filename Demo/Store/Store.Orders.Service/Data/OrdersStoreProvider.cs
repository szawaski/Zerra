using Zerra.Repository;

namespace Store.Orders.Service.Data
{
    public sealed class OrdersStoreProvider<TModel> : TransactStoreProvider<OrdersDataContext, TModel>
        where TModel : class, new()
    {
        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
