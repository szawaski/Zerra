using Zerra;
using Zerra.Repository;
using Zerra.Repository.MsSql;
using Zerra.Logging;
using System;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsMsSqlDataContext : MsSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public PetsMsSqlDataContext()
        {
            var connectionString = Config.GetSetting("PetsSqlConnectionStringMSSQL");
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Missing Config PetsSqlConnectionStringMSSQL");
            this.connectionString = connectionString;
            _ = Log.InfoAsync($"PetsSqlConnectionStringMSSQL {this.connectionString}");
        }
    }
}
