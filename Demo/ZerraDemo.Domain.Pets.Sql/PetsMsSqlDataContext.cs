using Zerra;
using Zerra.Repository.MsSql;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsMsSqlDataContext : MsSqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public PetsMsSqlDataContext()
        {
            this.connectionString = Config.GetSetting("PetsSqlConnectionStringMSSQL");
        }
    }
}
