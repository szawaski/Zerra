using System;
using System.Collections.Generic;
using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.DataModels
{
    [DataSourceEntity("Species")]
    public class SpeciesDataModel
    {
        [Identity]
        public Guid ID { get; set; }
        public string Name { get; set; }

        [DataSourceRelation("SpeciesID")]
        public ICollection<BreedDataModel> Breeds { get; set; }
    }
}
