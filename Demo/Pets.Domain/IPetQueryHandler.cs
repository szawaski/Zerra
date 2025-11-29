using Pets.Domain.Models;
using Zerra.CQRS;

namespace Pets.Domain
{
    public interface IPetsQueryHandler : IQueryHandler
    {
        void GetVoid();
        int GetNonTask();
        Task GetTaskOnly();

        Task<SpeciesModel[]> GetSpecies();
        Task<BreedModel[]> GetBreeds();
        Task<BreedModel[]> GetBreedsBySpecies(Guid SpeciesId);
        Task<List<PetModel>> GetPets();
        Task<PetModel> GetPet(Guid id);
        Task<bool> IsHungry(Guid id);
        Task<bool> NeedsToPoop(Guid id);
    }
}
