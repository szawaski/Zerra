using Zerra;
using ZerraDemo.Domain.Pets.DataModels;
using ZerraDemo.Domain.Pets.Models;

namespace ZerraDemo.Domain.Pets
{
    public class PetMap : IMapDefinition<PetDataModel, PetModel>
    {
        public void Define(IMapSetup<PetDataModel, PetModel> map)
        {
            map.Define(x => x.Breed, x => x.Breed!.Name);
            map.Define(x => x.Species, x => x.Breed!.Species!.Name);
        }
    }
}
