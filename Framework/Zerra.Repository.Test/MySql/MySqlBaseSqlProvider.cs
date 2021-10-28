// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public abstract partial class MySqlBaseSqlProvider<TModel> : TransactStoreProvider<MySqlTestSqlDataContext, TModel> where TModel : class, new()
    {
        protected override bool DisableQueryLinking => true;
        protected override bool DisableEventLinking => true;
        protected override bool DisablePersistLinking => true;
    }
}
