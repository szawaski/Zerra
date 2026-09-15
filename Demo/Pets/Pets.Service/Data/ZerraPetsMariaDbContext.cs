using MySqlConnector;
using Zerra.Repository.MariaDb;

namespace Pets.Service.Data
{    public sealed class ZerraPetsMariaDbContext : MariaDbDataContext
    {
        public override string GetConnectionString() => connectionString;

        private readonly string connectionString;
        public ZerraPetsMariaDbContext()
        {
            this.connectionString = "Server=localhost;Port=3307;Uid=root;Pwd=password123;Database=ZerraPets";
        }


        public static async Task DeleteMariaDb(MariaDbEngine engine)
        {
            var builder = new MySqlConnectionStringBuilder(engine.GetConnectionString());
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
    }
}