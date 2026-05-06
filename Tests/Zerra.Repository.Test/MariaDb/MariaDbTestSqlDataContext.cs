// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Repository.MariaDb;

namespace Zerra.Repository.Test
{
    public class MariaDbTestSqlDataContext : MariaDbDataContext
    {
        public override string GetConnectionString() => "Server=localhost;Port=3307;Uid=root;Pwd=password123;Database=ZerraSqlTest";
    }
}
