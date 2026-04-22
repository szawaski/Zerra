using Zerra.Repository;

namespace Pets.Service.Data
{
    [Entity("Pet")]
    public sealed class PetDataModel
    {
        [Identity(true)]
        public int Id { get; set; }

        [StoreProperties(true, 128)]
        public string? Name { get; set; }

        public int PetTypeId { get; set; }

        [Relation(nameof(PetTypeId))]
        public PetTypeDataModel? PetType { get; set; }
    }
}
