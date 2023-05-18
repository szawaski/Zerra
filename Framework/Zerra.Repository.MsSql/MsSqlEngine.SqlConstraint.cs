// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.MsSql
{
    public sealed partial class MsSqlEngine
    {
        private sealed class SqlConstraint
        {
            public string FK_Name { get; set; }
            public string FK_Schema { get; set; }
            public string FK_Table { get; set; }
            public string FK_Column { get; set; }
            public string PK_Name { get; set; }
            public string PK_Schema { get; set; }
            public string PK_Table { get; set; }
            public string PK_Column { get; set; }
        }
    }
}