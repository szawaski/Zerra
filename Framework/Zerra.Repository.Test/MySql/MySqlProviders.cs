// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.MySql;

namespace Zerra.Repository.Test
{
    public class MySqlTestSqlDataContext : MySqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => "Server=localhost;Port=3306;Uid=root;Pwd=password123;Database=ZerraSqlTest";
    }

    public abstract partial class MySqlBaseSqlProvider<TModel> : TransactStoreProvider<MySqlTestSqlDataContext, TModel> where TModel : class, new()
    {
        protected override bool DisableQueryLinking => true;
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }
    public class MySqlTestTypesCustomerSqlProvider : MySqlBaseSqlProvider<TestTypesModel> { }
}
