using Zerra.Repository;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsSqlProvider<TModel> : TransactStoreProvider<TModel>
        where TModel : class, new()
    {
        public ZerraPetsSqlProvider(ITransactStoreEngine engine) : base(engine) { }

        protected override bool EventLinking => false;
        protected override bool QueryLinking => true;
        protected override bool PersistLinking => true;
    }
}
