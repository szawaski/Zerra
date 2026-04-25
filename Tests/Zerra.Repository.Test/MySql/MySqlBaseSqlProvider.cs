// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class MySqlBaseSqlProvider<TModel> : TransactStoreProvider<MySqlTestSqlDataContext, TModel> where TModel : class, new()
    {

    }
}
