using System;
using Zerra;
using Zerra.Repository;
using Zerra.Repository.EventStoreDB;
using ZerraDemo.Domain.Ledger1.DataModels;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    [EventStoreEntity<Ledger1DataModel>]
    public class Ledger1EventStoreDataContext : EventStoreDBDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;
        public override bool Insecure => true;

        private readonly string connectionString;
        public Ledger1EventStoreDataContext()
        {
            var connectionString = Config.GetSetting("Ledger1EventStoreServer");
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Missing Config Ledger1EventStoreServer");
            this.connectionString = connectionString;
        }
    }
}
