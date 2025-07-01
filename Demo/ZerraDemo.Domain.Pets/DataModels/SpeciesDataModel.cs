using System;
using System.Collections.Generic;
using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.DataModels
{
    [Entity("Species")]
    public class SpeciesDataModel
    {
        [Identity]
        public Guid ID { get; set; }
        public string? Name { get; set; }

        [Relation(nameof(BreedDataModel.SpeciesID))]
        public ICollection<BreedDataModel>? Breeds { get; set; }
    }
}
