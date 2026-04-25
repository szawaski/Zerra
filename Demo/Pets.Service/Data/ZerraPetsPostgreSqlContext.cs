using Npgsql;
using Zerra.Repository.PostgreSql;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsPostgreSqlContext : PostgreSqlDataContext
    {
        public override string GetConnectionString() => connectionString;

        private readonly string connectionString;
        public ZerraPetsPostgreSqlContext()
        {
            this.connectionString = "Host=localhost;Port=5432;User ID=postgres;Password=password123;Database=zerrapets";
        }

        public static async Task DeletePostgreSql(PostgreSqlEngine engine)
        {
            var builder = new NpgsqlConnectionStringBuilder(engine.GetConnectionString());
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