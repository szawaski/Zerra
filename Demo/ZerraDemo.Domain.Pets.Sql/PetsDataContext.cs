using System.Collections.Generic;
using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsDataContext : DataContextSelector<ITransactStoreEngine>
    {
        protected override ICollection<DataContext<ITransactStoreEngine>> GetDataContexts()
        {
            return new DataContext<ITransactStoreEngine>[]
            {
                new PetsMsSqlDataContext(),
                new PetsPostgreSqlDataContext(),
                new PetsMySqlDataContext()
            };
        }
    }
}
