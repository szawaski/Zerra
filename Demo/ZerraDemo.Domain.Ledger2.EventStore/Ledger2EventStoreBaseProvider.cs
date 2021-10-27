using Zerra.Repository.EventStore;

namespace ZerraDemo.Domain.Ledger2.EventStore
{
    public class Ledger2EventStoreBaseProvider<TModel> : BaseEventStoreContextProvider<Ledger2EventStoreDataContext, TModel> where TModel : AggregateRoot
    {
    }
}
