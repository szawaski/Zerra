using Pets.Domain;
using Pets.Domain.Models;
using Zerra.CQRS;

namespace Pets.Service
{
    public sealed class PetsQueryHandler : BaseHandler, IPetsQueryHandler
    {
        public void GetVoid()
        {

        }

        public int GetNonTask()
        {
            return 1;
        }

        public Task GetTaskOnly()
        {
            return Task.CompletedTask;
        }

        public Task<SpeciesModel[]> GetSpecies()
        {
            var things = Context.Get<IThing>();
            return Task.FromResult(DataSource.Species.ToArray());
        }

        public Task<BreedModel[]> GetBreeds()
        {
            return Task.FromResult(DataSource.Breeds);
        }

        public Task<BreedModel[]> GetBreedsBySpecies(Guid SpeciesId)
        {
            return Task.FromResult(DataSource.Breeds.Where(b => b.SpeciesID == SpeciesId).ToArray());
        }

        public Task<PetModel> GetPet(Guid id)
        {
            return Task.FromResult(DataSource.Pets.First(p => p.ID == id));
        }

        public Task<List<PetModel>> GetPets()
        {
            return Task.FromResult<List<PetModel>>(DataSource.Pets.ToList());
        }

        public Task<bool> IsHungry(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> NeedsToPoop(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
