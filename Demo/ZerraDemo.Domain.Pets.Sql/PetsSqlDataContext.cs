using Zerra;
using Zerra.Repository.Sql.MsSql;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsSqlDataContext : MsSqlDataContext
    {
        public override string ConnectionString => connectionString;
        private readonly string connectionString;
        public PetsSqlDataContext()
        {
            this.connectionString = Config.GetSetting("PetsSqlConnectionString");
            AssureDataStore();
        }
    }
}
