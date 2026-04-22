using Zerra.Repository;

namespace Pets.Service.Data
{
    [Entity("PetType")]
    public sealed class PetTypeDataModel
    {
        [Identity(true)]
        public int Id { get; set; }

        [StoreProperties(true, 64)]
        public string? Name { get; set; }
    }
}
