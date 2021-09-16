using Zerra.Repository.Sql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1SqlBaseProvider<TModel> : SqlProvider<Ledger1SqlDataContext, TModel> where TModel : class, new()
    {
    }
}
