using System;
using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.DataModels
{
    [Entity("Breeds")]
    public class BreedDataModel
    {
        [Identity]
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Guid SpeciesID { get; set; }

        [Relation("SpeciesID")]
        public SpeciesDataModel Species { get; set; }
    }
}
