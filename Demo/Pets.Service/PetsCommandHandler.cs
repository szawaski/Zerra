using Pets.Domain;
using Pets.Domain.Commands;
using Pets.Domain.Models;
using Zerra.CQRS;

namespace Pets.Service
{
    public sealed class PetsCommandHandler : BaseHandler, IPetsCommandHandler
    {
        public async Task<int> Handle(AdoptPetCommand command, CancellationToken cancellationToken)
        {
            var thing = Context.Get<IThing>();
            Context.Log?.Trace(thing.Text);
            
            var breeds = await Context.Bus.Call<IPetsQueryHandler>().GetBreeds();

            if (!breeds.Any(x =>  x.ID == command.BreedID))
                throw new InvalidOperationException("BreedID not found");

            DataSource.Pets.Add(new PetModel()
            {
                ID = command.PetID,
                BreedID = command.BreedID,
                Name = command.Name
            });
            return DataSource.Pets.Count;
        }
    }
}
