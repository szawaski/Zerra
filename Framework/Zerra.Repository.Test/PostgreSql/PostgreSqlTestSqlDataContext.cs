// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.PostgreSql;

namespace Zerra.Repository.Test
{
    [ApplyEntity(typeof(PostgreSqlTestTypesModel))]
    [ApplyEntity(typeof(PostgreSqlTestRelationsModel))]
    public class PostgreSqlTestSqlDataContext : PostgreSqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => "User ID=postgres;Password=password123;Host=localhost;Port=5432;Database=zerrasqltest;";
    }
}
