using Microsoft.Data.SqlClient;
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

            if (!breeds.Any(x =>  x.ID == command.BreedID))
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
            await Repo.PersistAsync(new Create(model));

            var model2 = await Repo.QuerySingleAsync<PetTypeDataModel>(x => x.Name == command.Name);
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
            await Repo.PersistAsync(new Create(model));

            var model2 = await Repo.QuerySingleAsync<PetDataModel>(x => x.Name == command.Name);
            if (model2 == null)
                throw new InvalidOperationException("Failed to retrieve the pet after creation.");
            return model2.Id;
        }

        public async Task Handle(DeleteTestDatabaseCommand command, CancellationToken cancellationToken)
        {
            var context = new ZerraPetsDbContext();
            var builder = new SqlConnectionStringBuilder(context.ConnectionString);
            var testDatabase = builder.InitialCatalog;
            builder.InitialCatalog = "master";
            var connectionStringForMaster = builder.ToString();
            using (var sqlConnection = new SqlConnection(connectionStringForMaster))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"IF EXISTS(SELECT[dbid] FROM master.dbo.sysdatabases where[name] = '{testDatabase}')\r\nBEGIN\r\nALTER DATABASE [{testDatabase}] SET single_user WITH ROLLBACK IMMEDIATE\r\nDROP DATABASE {testDatabase}\r\nEND";
                    _ = await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
