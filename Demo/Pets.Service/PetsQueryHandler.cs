using Pets.Domain;
using Pets.Domain.Models;
using Pets.Service.Data;
using Pets.Service.Services;
using Zerra.Repository;

namespace Pets.Service
{
    public sealed class PetsQueryHandler : BaseHandlerWithRepo, IPetsQueryHandler
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
            var things = Context.GetService<IThing>();
            return Task.FromResult(StaticDataSource.Species.ToArray());
        }

        public Task<BreedModel[]> GetBreeds()
        {
            return Task.FromResult(StaticDataSource.Breeds);
        }

        public Task<BreedModel[]> GetBreedsBySpecies(Guid SpeciesId)
        {
            return Task.FromResult(StaticDataSource.Breeds.Where(b => b.SpeciesID == SpeciesId).ToArray());
        }

        public Task<PetModel> GetPet(Guid id)
        {
            return Task.FromResult(StaticDataSource.Pets.First(p => p.ID == id));
        }

        public Task<List<PetModel>> GetPets()
        {
            return Task.FromResult<List<PetModel>>(StaticDataSource.Pets.ToList());
        }

        public Task<bool> IsHungry(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> NeedsToPoop(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<PetSimpleModel[]> GetPetsFromRepo()
        {
            var items = await Repo.QueryManyAsync<PetDataModel>();
            var models = new PetSimpleModel[items.Count];
            var i = 0;
            foreach (var item in items)
                models[i++] = new PetSimpleModel() { ID = item.Id, Name = item.Name, Type = item.PetType?.Name };
            return models;
        }
    }
}
