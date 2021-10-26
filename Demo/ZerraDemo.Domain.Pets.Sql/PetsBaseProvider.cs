using Zerra.Repository.Sql;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsBaseProvider<TModel> : SqlProvider<PetsDataContext, TModel> where TModel : class, new()
    {
    }
}
