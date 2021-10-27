using Zerra;
using Zerra.Repository.MsSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1MsSqlDataContext : MsSqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1MsSqlDataContext()
        {
            this.connectionString = Config.GetSetting("Ledger1SqlConnectionStringPOSTGRESQL");
        }
    }
}
