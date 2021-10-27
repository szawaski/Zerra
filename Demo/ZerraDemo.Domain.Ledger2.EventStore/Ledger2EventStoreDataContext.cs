using Zerra;
using Zerra.Repository.EventStoreDB;

namespace ZerraDemo.Domain.Ledger2.EventStore
{
    public class Ledger2EventStoreDataContext : EventStoreDBDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => connectionString;
        public override bool Insecure => true;
        private readonly string connectionString;
        public Ledger2EventStoreDataContext()
        {
            this.connectionString = Config.GetSetting("Ledger2EventStoreServer");
        }
    }
}
