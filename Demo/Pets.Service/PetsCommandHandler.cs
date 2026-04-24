using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using Pets.Domain;
using Pets.Domain.Commands;
using Pets.Domain.Models;
using Pets.Service.Data;
using Pets.Service.Services;
using Zerra.Repository;
using Zerra.Repository.MsSql;
using Zerra.Repository.MySql;
using Zerra.Repository.PostgreSql;

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
            if (engine is MsSqlEngine)
                await DeleteMsSql();
            else if (engine is MySqlEngine)
                await DeleteMySql();
            else if (engine is PostgreSqlEngine)
                await DeletePostgreSql();
        }

        private async Task DeleteMsSql()
        {
            var context = new ZerraPetsMsSqlContext();
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

        private async Task DeleteMySql()
        {
            var context = new ZerraPetsMySqlContext();
            var builder = new MySqlConnectionStringBuilder(context.ConnectionString);
            var testDatabase = builder.Database;
            builder.Database = "sys";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new MySqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT CONCAT('KILL ', id, ';') FROM INFORMATION_SCHEMA.PROCESSLIST WHERE `db` = '{testDatabase}'; DROP DATABASE IF EXISTS {testDatabase};";
                    _ = command.ExecuteNonQuery();
                }
            }
        }

        private async Task DeletePostgreSql()
        {
            var context = new ZerraPetsPostgreSqlContext();
            var builder = new NpgsqlConnectionStringBuilder(context.ConnectionString);
            var testDatabase = builder.Database;
            builder.Database = "postgres";
            var connectionStringForMaster = builder.ToString();
            using (var connection = new NpgsqlConnection(connectionStringForMaster))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname='{testDatabase}';";
                    _ = command.ExecuteNonQuery();
                    command.CommandText = $"DROP DATABASE IF EXISTS {testDatabase};";
                    _ = command.ExecuteNonQuery();
                }
            }
        }
    }
}
