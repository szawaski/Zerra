using System.Collections.Generic;
using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsDataContext : DataContextSelector
    {
        protected override ICollection<DataContext> GetDataContexts()
        {
            return new DataContext[]
            {
                new PetsMsSqlDataContext(),
                new PetsPostgreSqlDataContext(),
                new PetsMySqlDataContext()
            };
        }
    }
}
