// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.Sql.MsSql;

namespace Zerra.Repository.Sql.Test
{
    public class TestSqlDataContext : MsSqlDataContext
    {
        public override string ConnectionString => "data source=.;initial catalog=ZerraSqlTest;integrated security=True;MultipleActiveResultSets=True;";
    }

    public abstract partial class BaseSqlProvider<TModel> : SqlProvider<TestSqlDataContext, TModel> where TModel : class, new()
    {
        protected override bool DisableQueryLinking => true;
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }
    public class TestTypesCustomerSqlProvider : BaseSqlProvider<TestTypesModel> { }
}
