using Zerra;
using Zerra.Repository;
using Zerra.Repository.EventStoreDB;
using ZerraDemo.Domain.Ledger1.DataModels;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    [ApplyEntity(typeof(Ledger1DataModel))]
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
