using Zerra.CQRS;

namespace Pets.Domain.Commands
{
    public sealed class AddPetCommand : ICommand<int>
    {
        public required string Name { get; set; }
        public required int PetTypeId { get; set; }
    }
}
