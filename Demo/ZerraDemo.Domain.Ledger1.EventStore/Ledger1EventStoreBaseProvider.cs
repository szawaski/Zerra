using Zerra.Repository.EventStore;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1EventStoreBaseProvider<TModel> : EventStoreProvider<Ledger1EventStoreDataContext, TModel> where TModel : class, new()
    {

    }
}
