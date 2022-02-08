using Zerra;
using Zerra.Repository;
using Zerra.Repository.PostgreSql;

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
        }
    }
}
