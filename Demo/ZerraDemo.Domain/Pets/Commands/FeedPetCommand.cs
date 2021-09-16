using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Pets.Commands
{
    [ServiceExposed]
    public class FeedPetCommand : ICommand
    {
        public Guid PetID { get; set; }
        public int Amount { get; set; }
    }
}
