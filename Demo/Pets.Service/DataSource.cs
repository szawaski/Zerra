using Pets.Domain.Models;

namespace Pets.Service
{
    public static class DataSource
    {
        public static readonly SpeciesModel[] Species =
        [
            new SpeciesModel() { ID = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Dog" },
            new SpeciesModel() { ID = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Cat" },
            new SpeciesModel() { ID = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Bird" },
        ];

        public static readonly BreedModel[] Breeds =
        [
            new BreedModel(){ ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), SpeciesID = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Border Collie" },
            new BreedModel(){ ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), SpeciesID = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Brittany Spaniel" },
            new BreedModel(){ ID = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), SpeciesID = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Russian Blue" },
            new BreedModel(){ ID = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), SpeciesID = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Tabby" },
            new BreedModel(){ ID = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), SpeciesID = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Parakeet" },
            new BreedModel(){ ID = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), SpeciesID = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Canary" }
        ];

        public static readonly List<PetModel> Pets = new();
    }
}
