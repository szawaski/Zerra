using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsBaseProvider<TModel> : TransactStoreProvider<PetsDataContext, TModel> where TModel : class, new()
    {
    }
}
