using Zerra;
using Zerra.Repository.EventStoreDB;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1EventStoreDataContext : EventStoreDBDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => connectionString;
        public override bool Insecure => true;
        private readonly string connectionString;
        public Ledger1EventStoreDataContext()
        {
            this.connectionString = Config.GetSetting("Ledger1EventStoreServer");
        }
    }
}
