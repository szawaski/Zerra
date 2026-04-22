using Zerra.CQRS;

namespace Pets.Domain.Commands
{
    public sealed class AddPetTypeCommand : ICommand<int>
    {
        public required string Name { get; set; }
    }
}
