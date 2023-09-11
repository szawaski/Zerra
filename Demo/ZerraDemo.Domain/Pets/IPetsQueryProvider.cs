using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra.CQRS;
using ZerraDemo.Domain.Pets.Models;

namespace ZerraDemo.Domain.Pets
{
    [ServiceExposed]
    public interface IPetsQueryProvider
    {
        Task<ICollection<SpeciesModel>> GetSpecies();
        Task<ICollection<BreedModel>> GetBreeds(Guid speciesID);
        Task<ICollection<PetModel>> GetPets();
        Task<PetModel> GetPet(Guid id);
        Task<bool> IsHungry(Guid id);
        Task<bool> NeedsToPoop(Guid id);
    }
}
