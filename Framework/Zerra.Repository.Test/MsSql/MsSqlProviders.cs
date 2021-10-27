// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.MsSql;

namespace Zerra.Repository.Test
{
    public class MsSqlTestSqlDataContext : MsSqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => "data source=.;initial catalog=ZerraSqlTest;integrated security=True;MultipleActiveResultSets=True;";
    }

    public abstract partial class MsSqlBaseSqlProvider<TModel> : TransactStoreProvider<MsSqlTestSqlDataContext, TModel> where TModel : class, new()
    {
        protected override bool DisableQueryLinking => true;
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }
    public class MsSqlTestTypesCustomerSqlProvider : MsSqlBaseSqlProvider<TestTypesModel> { }
}
