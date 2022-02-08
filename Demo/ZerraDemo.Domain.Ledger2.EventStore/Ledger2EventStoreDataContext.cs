using Zerra;
using Zerra.Repository;
using Zerra.Repository.EventStoreDB;
using ZerraDemo.Domain.Ledger2.Aggregates;

namespace ZerraDemo.Domain.Ledger2.EventStore
{
    [EventStoreAggregate(typeof(Transaction2Aggregate))]
    public class Ledger2EventStoreDataContext : EventStoreDBDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;
        public override bool Insecure => true;

        private readonly string connectionString;
        public Ledger2EventStoreDataContext()
        {
            this.connectionString = Config.GetSetting("Ledger2EventStoreServer");
        }
    }
}
