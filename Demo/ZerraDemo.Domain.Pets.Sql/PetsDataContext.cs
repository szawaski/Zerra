using System.Collections.Generic;
using Zerra.Repository;
using ZerraDemo.Domain.Pets.DataModels;

namespace ZerraDemo.Domain.Pets.Sql
{
    [TransactStoreEntity(typeof(BreedDataModel))]
    [TransactStoreEntity(typeof(PetDataModel))]
    [TransactStoreEntity(typeof(SpeciesDataModel))]
    public class PetsDataContext : DataContextSelector
    {
        protected override ICollection<DataContext> LoadDataContexts()
        {
            return new DataContext[]
            {
                new PetsPostgreSqlDataContext(),
                new PetsMySqlDataContext(),
                new PetsMsSqlDataContext(),                
            };
        }
    }
}
