using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Pets.Commands
{
    [ServiceExposed]
    [ServiceSecure]
    public class LetPetOutToPoopCommand : ICommand
    {
        public Guid PetID { get; set; }
    }
}
