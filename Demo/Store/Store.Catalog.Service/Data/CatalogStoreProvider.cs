using Zerra.Repository;

namespace Store.Catalog.Service.Data
{
    public sealed class CatalogStoreProvider<TModel> : TransactStoreProvider<CatalogDataContext, TModel>
        where TModel : class, new()
    {
        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => false;
    }
}
