// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.PostgreSql;

namespace Zerra.Repository.Test
{
    [TransactStoreEntity<PostgreSqlTestTypesModel>]
    [TransactStoreEntity<PostgreSqlTestRelationsModel>]
    public class PostgreSqlTestSqlDataContext : PostgreSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => "User ID=postgres;Password=password123;Host=localhost;Port=5432;Database=zerrasqltest;";
    }
}
