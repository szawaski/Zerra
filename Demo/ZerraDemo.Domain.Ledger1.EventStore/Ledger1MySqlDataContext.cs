using System;
using Zerra;
using Zerra.Repository;
using Zerra.Repository.MsSql;

namespace ZerraDemo.Domain.Ledger1.EventStore
{
    public class Ledger1MySqlDataContext : MsSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public Ledger1MySqlDataContext()
        {
            var connectionString = Config.GetSetting("Ledger1SqlConnectionStringMYSQL");
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Missing Config Ledger1SqlConnectionStringMYSQL");
            this.connectionString = connectionString;
        }
    }
}
