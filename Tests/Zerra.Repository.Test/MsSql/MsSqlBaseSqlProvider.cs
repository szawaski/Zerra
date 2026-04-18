// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class MsSqlBaseSqlProvider<TModel> : TransactStoreProvider<MsSqlTestSqlDataContext, TModel> where TModel : class, new()
    {

    }
}
