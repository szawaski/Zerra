using Zerra;
using Zerra.Repository.PostgreSql;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsPostgreSqlDataContext : PostgreSqlDataContext
    {
        protected override bool DisableAssureDataStore => false;
        public override string ConnectionString => connectionString;
        private readonly string connectionString;
        public PetsPostgreSqlDataContext()
        {
            this.connectionString = Config.GetSetting("PetsSqlConnectionStringPOSTGRESQL");
        }
    }
}
