using Zerra.Repository;

namespace ZerraDemo.Domain.Ledger2.EventStore
{
    public class Ledger2EventStoreBaseProvider<TModel> : BaseEventStoreEngineProvider<Ledger2EventStoreDataContext, TModel> where TModel : AggregateRoot
    {
    }
}
