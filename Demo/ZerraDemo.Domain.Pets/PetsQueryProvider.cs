using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zerra;
using Zerra.Repository;
using ZerraDemo.Domain.Pets.DataModels;
using ZerraDemo.Domain.Pets.Models;
using ZerraDemo.Common;
using Zerra.Map;

namespace ZerraDemo.Domain.Pets
{

    public class PetsQueryProvider : IPetsQueryProvider
    {
        public PetsQueryProvider()
        {
            PetsAssureData.AssureData();
        }

        public async Task<ICollection<SpeciesModel>> GetSpecies()
        {
            Access.CheckRole("Admin");

            var items = await Repo.QueryAsync(new QueryMany<SpeciesDataModel>());
            var models = items.Map<SpeciesModel[]>();
            return models;
        }

        public async Task<ICollection<BreedModel>> GetBreeds(Guid speciesID)
        {
            Access.CheckRole("Admin");

            var items = await Repo.QueryAsync(new QueryMany<BreedDataModel>(x => x.SpeciesID == speciesID));
            var models = items.Map<BreedModel[]>();
            return models;
        }

        public async Task<ICollection<PetModel>> GetPets()
        {
            Access.CheckRole("Admin");

            var items = await Repo.QueryAsync(new QueryMany<PetDataModel>(
                new Graph<PetDataModel>(
                    true,
                    x => x.Breed,
                    x => x.Breed!.Species
            )));

            var models = items.Map<PetModel[]>();
            foreach (var model in models)
            {
                if (model.AmountEaten.HasValue && model.LastEaten.HasValue)
                    model.AmountEaten = Math.Max(0, model.AmountEaten.Value - (DateTime.UtcNow - model.LastEaten.Value).Hours);
            }
            return models;
        }

        public async Task<PetModel> GetPet(Guid id)
        {
            Access.CheckRole("Admin");

            var item = await Repo.QueryAsync(new QuerySingle<PetDataModel>(
                x => x.ID == id,
                new Graph<PetDataModel>(
                    true,
                    x => x.Breed,
                    x => x.Breed!.Species
            )));
            if (item == null)
                throw new Exception("Pet not found");

            var model = item.Map<PetModel>();
            return model;
        }

        public async Task<bool> IsHungry(Guid id)
        {
            Access.CheckRole("Admin");

            var item = await Repo.QueryAsync(new QuerySingle<PetDataModel>(
                 x => x.ID == id,
                 new Graph<PetDataModel>(
                     x => x.AmountEaten,
                     x => x.LastEaten
            )));
            if (item == null)
                throw new Exception("Pet not found");

            if (item.AmountEaten.HasValue && item.LastEaten.HasValue)
                item.AmountEaten = Math.Max(0, item.AmountEaten.Value - (DateTime.UtcNow - item.LastEaten.Value).Hours);

            return (item.AmountEaten ?? 0) == 0;
        }

        public async Task<bool> NeedsToPoop(Guid id)
        {
            Access.CheckRole("Admin");

            var item = await Repo.QueryAsync(new QuerySingle<PetDataModel>(
                x => x.ID == id,
                new Graph<PetDataModel>(
                    x => x.LastPooped
            )));
            if (item == null)
                throw new Exception("Pet not found");

            var hoursSincePooped = (int)(DateTime.UtcNow - (item.LastPooped ?? DateTime.MinValue)).TotalHours;

            return hoursSincePooped > 4;
        }
    }
}
