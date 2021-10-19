// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.Sql.MsSql;

namespace Zerra.Repository.Sql.Test
{
    public class PostgreSqlTestSqlDataContext : MsSqlDataContext
    {
        public override string ConnectionString => "User ID=postgres;Password=password123;Host=localhost;Port=5432;Database=zerrasqltest;";
    }

    public abstract partial class PostgreSqlBaseSqlProvider<TModel> : SqlProvider<PostgreSqlTestSqlDataContext, TModel> where TModel : class, new()
    {
        protected override bool DisableQueryLinking => true;
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }
    public class PostgreSqlTestTypesCustomerSqlProvider : PostgreSqlBaseSqlProvider<TestTypesModel> { }
}
