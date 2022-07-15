using System;
using System.Linq;
using Zerra.Repository;
using ZerraDemo.Domain.Pets.DataModels;

namespace ZerraDemo.Domain.Pets
{
    public static class PetsAssureData
    {
        private static readonly object assureDataLock = new();
        public static void AssureData()
        {
            lock (assureDataLock)
            {
                var species = Repo.Query(new QueryMany<SpeciesDataModel>());

                if (!species.Any(x => x.Name == "Dog"))
                    Repo.Persist(new Create<SpeciesDataModel>(new SpeciesDataModel() { ID = Guid.NewGuid(), Name = "Dog" }));
                if (!species.Any(x => x.Name == "Bear"))
                    Repo.Persist(new Create<SpeciesDataModel>(new SpeciesDataModel() { ID = Guid.NewGuid(), Name = "Bear" }));
                if (!species.Any(x => x.Name == "Magical"))
                    Repo.Persist(new Create<SpeciesDataModel>(new SpeciesDataModel() { ID = Guid.NewGuid(), Name = "Magical" }));

                species = Repo.Query(new QueryMany<SpeciesDataModel>());
                var speciesDogID = species.First(x => x.Name == "Dog").ID;
                var speciesBearID = species.First(x => x.Name == "Bear").ID;
                var speciesMagicalID = species.First(x => x.Name == "Magical").ID;

                var breeds = Repo.Query(new QueryMany<BreedDataModel>());

                if (!breeds.Any(x => x.SpeciesID == speciesDogID && x.Name == "Brittany Spaniel"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesDogID, Name = "Brittany Spaniel" }));
                if (!breeds.Any(x => x.SpeciesID == speciesDogID && x.Name == "Border Collie"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesDogID, Name = "Border Collie" }));
                if (!breeds.Any(x => x.SpeciesID == speciesDogID && x.Name == "Rodesian Ridgeback"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesDogID, Name = "Rodesian Ridgeback" }));
                if (!breeds.Any(x => x.SpeciesID == speciesDogID && x.Name == "Husky"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesDogID, Name = "Husky" }));

                if (!breeds.Any(x => x.SpeciesID == speciesBearID && x.Name == "Black"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesBearID, Name = "Black" }));
                if (!breeds.Any(x => x.SpeciesID == speciesBearID && x.Name == "Brown"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesBearID, Name = "Brown" }));
                if (!breeds.Any(x => x.SpeciesID == speciesBearID && x.Name == "Grizzley"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesBearID, Name = "Grizzley" }));
                if (!breeds.Any(x => x.SpeciesID == speciesBearID && x.Name == "Kodiak"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesBearID, Name = "Kodiak" }));
                if (!breeds.Any(x => x.SpeciesID == speciesBearID && x.Name == "Panda"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesBearID, Name = "Panda" }));
                if (!breeds.Any(x => x.SpeciesID == speciesBearID && x.Name == "Pooh"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesBearID, Name = "Pooh" }));

                if (!breeds.Any(x => x.SpeciesID == speciesMagicalID && x.Name == "Unicorn"))
                    Repo.Persist(new Create<BreedDataModel>(new BreedDataModel() { ID = Guid.NewGuid(), SpeciesID = speciesMagicalID, Name = "Unicorn" }));

                breeds = Repo.Query(new QueryMany<BreedDataModel>());
                var breedBorderCollieID = breeds.First(x => x.Name == "Border Collie").ID;

                var pets = Repo.Query(new QueryMany<PetDataModel>());
                if (!pets.Any(x => x.Name == "Lucy"))
                    Repo.Persist(new Create<PetDataModel>(new PetDataModel() { ID = Guid.NewGuid(), BreedID = breedBorderCollieID, Name = "Lucy" }));
            }
        }
    }
}
