using Zerra.Repository;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1BaseProvider<TModel> : TransactStoreProvider<Ledger1DataContext, TModel> where TModel : class, new()
    {
    }
}
