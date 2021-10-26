using Zerra.Repository.Sql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1BaseProvider<TModel> : SqlProvider<Ledger1DataContext, TModel> where TModel : class, new()
    {
    }
}
