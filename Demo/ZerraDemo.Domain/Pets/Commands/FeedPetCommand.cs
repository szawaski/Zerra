using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Pets.Commands
{
    [ServiceExposed]
    [ServiceSecure("Admin")]
    public class FeedPetCommand : ICommand<FeedPetCommandResult>
    {
        public Guid PetID { get; set; }
        public int Amount { get; set; }
    }
}
