using Zerra.CQRS;

namespace Pets.Domain.Commands
{
    public class AdoptPetCommand : ICommand<int>
    {
        public Guid PetID { get; set; }
        public Guid BreedID { get; set; }
        public string? Name { get; set; }
    }
}
