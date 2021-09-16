using Zerra.Repository.Sql;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsSqlBaseProvider<TModel> : SqlProvider<PetsSqlDataContext, TModel> where TModel : class, new()
    {
    }
}
