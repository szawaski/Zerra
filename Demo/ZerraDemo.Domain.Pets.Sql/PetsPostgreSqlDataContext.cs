using Zerra;
using Zerra.Repository;
using Zerra.Repository.PostgreSql;
using Zerra.Logging;
using System;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsPostgreSqlDataContext : PostgreSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;
        private readonly string connectionString;
        public PetsPostgreSqlDataContext()
        {
            var connectionString = Config.GetSetting("PetsSqlConnectionStringPOSTGRESQL");
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Missing Config PetsSqlConnectionStringPOSTGRESQL");
            this.connectionString = connectionString;
            _ = Log.InfoAsync($"PetsSqlConnectionStringPOSTGRESQL {this.connectionString}");
        }
    }
}
