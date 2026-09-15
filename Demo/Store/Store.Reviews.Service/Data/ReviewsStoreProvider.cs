using Zerra.Repository;

namespace Store.Reviews.Service.Data
{
    public sealed class ReviewsStoreProvider<TModel> : TransactStoreProvider<ReviewsDataContext, TModel>
        where TModel : class, new()
    {
        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
