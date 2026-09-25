using Zerra.Repository;

namespace Store.Reviews.Service.Data
{
    public sealed class ReviewsStoreProvider<TModel> : TransactStoreProvider<ReviewsDataContext, TModel>
        where TModel : class, new()
    {
        public ReviewsStoreProvider() { }
        //a context of its own is a store of its own when it's in-memory, the tests give each test one
        public ReviewsStoreProvider(ReviewsDataContext context) : base(context) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
