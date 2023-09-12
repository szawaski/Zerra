using Zerra.CQRS;
using ZerraDemo.Domain.Pets.Commands;

namespace ZerraDemo.Domain.Pets
{
    public interface IPetsCommandHandler :
        ICommandHandler<AdoptPetCommand>,
        ICommandHandler<FeedPetCommand>,
        ICommandHandler<LetPetOutToPoopCommand>
    {
    }
}
