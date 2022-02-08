using Zerra;
using Zerra.Repository;
using Zerra.Repository.MsSql;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsMsSqlDataContext : MsSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public PetsMsSqlDataContext()
        {
            this.connectionString = Config.GetSetting("PetsSqlConnectionStringMSSQL");
        }
    }
}
