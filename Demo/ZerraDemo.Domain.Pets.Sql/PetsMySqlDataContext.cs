﻿using Zerra;
using Zerra.Repository;
using Zerra.Repository.MySql;
using Zerra.Logging;

namespace ZerraDemo.Domain.Pets.Sql
{
    public class PetsMySqlDataContext : MySqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => connectionString;

        private readonly string connectionString;
        public PetsMySqlDataContext()
        {
            this.connectionString = Config.GetSetting("PetsSqlConnectionStringMYSQL");
            _ = Log.InfoAsync($"PetsSqlConnectionStringMYSQL {this.connectionString}");
        }
    }
}
