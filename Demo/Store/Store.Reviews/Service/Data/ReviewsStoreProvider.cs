using Zerra.Repository;

namespace Store.Reviews.Service.Data
{
    public sealed class ReviewsStoreProvider<TModel> : TransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public ReviewsStoreProvider(ITransactStoreEngine engine) : base(engine) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
