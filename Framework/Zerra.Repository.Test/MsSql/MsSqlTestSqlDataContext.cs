﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.MsSql;

namespace Zerra.Repository.Test
{
    [ApplyEntity(typeof(MsSqlTestTypesModel), false, true, true)]
    [ApplyEntity(typeof(MsSqlTestRelationsModel), false)]
    public class MsSqlTestSqlDataContext : MsSqlDataContext
    {
        protected override bool DisableBuildStoreFromModels => false;
        public override string ConnectionString => "data source=.;initial catalog=ZerraSqlTest;integrated security=True;MultipleActiveResultSets=True;";
    }
}
