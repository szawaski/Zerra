// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.MsSql;

namespace Zerra.Repository.Test
{
    [TransactStoreEntity<MsSqlTestTypesModel>(false, true, true)]
    [TransactStoreEntity<MsSqlTestRelationsModel>(false)]
    public class MsSqlTestSqlDataContext : MsSqlDataContext
    {
        protected override DataStoreGenerationType DataStoreGenerationType => DataStoreGenerationType.CodeFirst;
        public override string ConnectionString => "data source=.;initial catalog=ZerraSqlTest;integrated security=True;MultipleActiveResultSets=True;";
    }
}
