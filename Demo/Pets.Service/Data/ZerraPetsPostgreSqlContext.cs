using Zerra.Repository.PostgreSql;

namespace Pets.Service.Data
{
    public sealed class ZerraPetsPostgreSqlContext : PostgreSqlDataContext
    {
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public ZerraPetsPostgreSqlContext()
        {
            this.connectionString = "Host=localhost;Port=5432;User ID=postgres;Password=password123;Database=zerrapets";
        }
    }
}