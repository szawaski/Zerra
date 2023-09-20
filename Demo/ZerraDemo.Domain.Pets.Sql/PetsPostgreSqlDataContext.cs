using Zerra;
using Zerra.Repository;
using Zerra.Repository.PostgreSql;
using Zerra.Logging;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsPostgreSqlDataContext : PostgreSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;
        private readonly string connectionString;
        public PetsPostgreSqlDataContext()
        {
            this.connectionString = Config.GetSetting("PetsSqlConnectionStringPOSTGRESQL");
            _ = Log.InfoAsync($"PetsSqlConnectionStringPOSTGRESQL {this.connectionString}");
        }
    }
}
