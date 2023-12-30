using System;
using Zerra.CQRS;

namespace ZerraDemo.Domain.Pets.Commands
{
    [ServiceExposed]
    public class AdoptPetCommand : ICommand
    {
        public Guid PetID { get; set; }
        public Guid BreedID { get; set; }
        public string? Name { get; set; }
    }
}
