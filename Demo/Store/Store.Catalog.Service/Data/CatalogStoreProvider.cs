using Zerra.Repository;

namespace Store.Catalog.Service.Data
{
    public sealed class CatalogStoreProvider<TModel> : TransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public CatalogStoreProvider(ITransactStoreEngine engine) : base(engine) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
