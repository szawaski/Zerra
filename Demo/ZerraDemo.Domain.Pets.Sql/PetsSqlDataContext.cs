using Zerra;
using Zerra.Repository.Sql.MsSql;
using Zerra.Repository.Sql.PostgreSql;

namespace ZerraDemo.Domain.Pets.Sql
{
    //public class PetsSqlDataContext : MsSqlDataContext
    public class PetsSqlDataContext : PostgreSqlDataContext
    {
        public override string ConnectionString => connectionString;
        private readonly string connectionString;
        public PetsSqlDataContext()
        {
            //this.connectionString = Config.GetSetting("PetsSqlConnectionStringMSSQL");
            this.connectionString = Config.GetSetting("PetsSqlConnectionStringPOSTGRESQL");
            AssureDataStore();
        }
    }
}
