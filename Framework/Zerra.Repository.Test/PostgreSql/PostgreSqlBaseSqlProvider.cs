// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public abstract partial class PostgreSqlBaseSqlProvider<TModel> : TransactStoreProvider<PostgreSqlTestSqlDataContext, TModel> where TModel : class, new()
    {

    }
}
