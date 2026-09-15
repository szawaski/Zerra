using Microsoft.Data.SqlClient;
using Zerra.Repository.MsSql;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsMsSqlContext : MsSqlDataContext
    {
        public override string GetConnectionString() => connectionString;

        private readonly string connectionString;
        public ZerraPetsMsSqlContext()
        {
            this.connectionString = "Data Source=.;Initial Catalog=ZerraPets;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        }

        public static async Task DeleteMsSql(MsSqlEngine engine)
        {
            var builder = new SqlConnectionStringBuilder(engine.GetConnectionString());
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