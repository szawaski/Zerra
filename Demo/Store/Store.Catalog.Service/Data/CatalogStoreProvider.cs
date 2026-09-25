using Zerra.Repository;

namespace Store.Catalog.Service.Data
{
    public sealed class CatalogStoreProvider<TModel> : TransactStoreProvider<CatalogDataContext, TModel>
        where TModel : class, new()
    {
        public CatalogStoreProvider() { }
        //a context of its own is a store of its own when it's in-memory, the tests give each test one
        public CatalogStoreProvider(CatalogDataContext context) : base(context) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
