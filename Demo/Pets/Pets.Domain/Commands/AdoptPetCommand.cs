using Zerra.CQRS;

namespace Pets.Domain.Commands
{
    public sealed class AdoptPetCommand : ICommand<int>
    {
        public required Guid PetID { get; set; }
        public required Guid BreedID { get; set; }
        public required string Name { get; set; }
    }
}
