using System;
using Zerra;
using Zerra.Repository;
using Zerra.Repository.MsSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1MsSqlDataContext : MsSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1MsSqlDataContext()
        {
            var connectionString = Config.GetSetting("Ledger1SqlConnectionStringMSSQL");
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Missing Config Ledger1SqlConnectionStringMSSQL");
            this.connectionString = connectionString;
        }
    }
}
