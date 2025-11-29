using Pets.Domain.Commands;
using Zerra.CQRS;

namespace Pets.Domain
{
    public interface IPetsCommandHandler :
        ICommandHandler<AdoptPetCommand, int>
    {
    }
}
