using Pets.Domain;
using Pets.Domain.Commands;
using Pets.Domain.Models;
using Pets.Service.Data;
using Pets.Service.Services;
using Zerra.Repository;

namespace Pets.Service
{
    public sealed class PetsCommandHandler : BaseHandlerWithRepo, IPetsCommandHandler
    {
        public async Task<int> Handle(AdoptPetCommand command, CancellationToken cancellationToken)
        {
            var thing = Context.GetService<IThing>();
            Log?.Trace($"Depencency Injection Check: {thing.Text}");

            var breeds = await Bus.Call<IPetsQueryHandler>().GetBreeds();

            if (!breeds.Any(x => x.ID == command.BreedID))
                throw new InvalidOperationException("BreedID not found");

            StaticDataSource.Pets.Add(new PetModel()
            {
                ID = command.PetID,
                BreedID = command.BreedID,
                Name = command.Name
            });
            return StaticDataSource.Pets.Count;
        }

        public async Task<int> Handle(AddPetTypeCommand command, CancellationToken cancellationToken)
        {
            var model = new PetTypeDataModel()
            {
                Name = command.Name,
            };
            await Repo.CreateAsync(model);

            var model2 = await Repo.SingleAsync<PetTypeDataModel>(x => x.Name == command.Name);
            if (model2 == null)
                throw new InvalidOperationException("Failed to retrieve the pet type after creation.");
            return model2.Id;
        }

        public async Task<int> Handle(AddPetCommand command, CancellationToken cancellationToken)
        {
            var model = new PetDataModel()
            {
                Name = command.Name,
                PetTypeId = command.PetTypeId,
            };
            await Repo.CreateAsync(model);

            var model2 = await Repo.SingleAsync<PetDataModel>(x => x.Name == command.Name);
            if (model2 == null)
                throw new InvalidOperationException("Failed to retrieve the pet after creation.");
            return model2.Id;
        }

        public async Task Handle(DeleteTestDatabaseCommand command, CancellationToken cancellationToken)
        {
            var context = new ZerraPetsDbContext();
            if (!context.TryGetEngine(out var engine))
                throw new InvalidOperationException();

            if (engine is Zerra.Repository.MsSql.MsSqlEngine msSqlEngine)
                await ZerraPetsMsSqlContext.DeleteMsSql(msSqlEngine);
            else if (engine is Zerra.Repository.MySql.MySqlEngine mySqlEngine)
                await ZerraPetsMySqlContext.DeleteMySql(mySqlEngine);
            else if (engine is Zerra.Repository.PostgreSql.PostgreSqlEngine postgreSqlEngine)
                await ZerraPetsPostgreSqlContext.DeletePostgreSql(postgreSqlEngine);
        }
    }
}
