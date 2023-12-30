using Zerra;
using Zerra.Repository;
using Zerra.Repository.MySql;
using Zerra.Logging;
using System;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsMySqlDataContext : MySqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public PetsMySqlDataContext()
        {
            var connectionString = Config.GetSetting("PetsSqlConnectionStringMYSQL");
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new Exception("Missing Config PetsSqlConnectionStringMYSQL");
            this.connectionString = connectionString;
            _ = Log.InfoAsync($"PetsSqlConnectionStringMYSQL {this.connectionString}");
        }
    }
}
