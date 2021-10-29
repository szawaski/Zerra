// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.MySql;

namespace Zerra.Repository.Test
{
    [TransactStoreEntity(typeof(MySqlTestTypesModel))]
    [TransactStoreEntity(typeof(MySqlTestRelationsModel))]
    public class MySqlTestSqlDataContext : MySqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => "Server=localhost;Port=3306;Uid=root;Pwd=password123;Database=ZerraSqlTest";
    }
}
