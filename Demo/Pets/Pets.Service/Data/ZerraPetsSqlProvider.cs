using Zerra.Repository;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsSqlProvider<TModel> : TransactStoreProvider<ZerraPetsSelectorDbContext, TModel>
        where TModel : class, new()
    {
        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => true;
    }
}
