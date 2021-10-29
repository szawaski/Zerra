using Zerra.Repository.EventStore;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1EventStoreBaseProvider<TModel> : EventStoreAsTransactStoreProvider<Ledger1EventStoreDataContext, TModel> where TModel : class, new()
    {

    }
}
