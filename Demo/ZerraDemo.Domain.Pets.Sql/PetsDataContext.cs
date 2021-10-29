using System.Collections.Generic;
using Zerra.Repository;
using ZerraDemo.Domain.Pets.DataModels;

namespace ZerraDemo.Domain.Pets.Sql
{
    [ApplyEntity(typeof(BreedDataModel))]
    [ApplyEntity(typeof(PetDataModel))]
    [ApplyEntity(typeof(SpeciesDataModel))]
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
