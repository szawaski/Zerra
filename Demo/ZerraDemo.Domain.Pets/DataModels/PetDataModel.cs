using System;
using Zerra.Repository;

namespace ZerraDemo.Domain.Pets.DataModels
{
    [Entity("Pets")]
    public class PetDataModel
    {
        [Identity]
        public Guid ID { get; set; }
        public string? Name { get; set; }
        public Guid BreedID { get; set; }

        public DateTime? LastEaten { get; set; }
        public int? AmountEaten { get; set; }

        public DateTime? LastPooped { get; set; }

        [Relation(nameof(BreedID))]
        public BreedDataModel? Breed { get; set; }
    }
}
