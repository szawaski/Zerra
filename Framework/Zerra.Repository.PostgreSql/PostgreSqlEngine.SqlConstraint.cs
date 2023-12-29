// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.PostgreSql
{
    public sealed partial class PostgreSqlEngine
    {
        private sealed class SqlConstraint
        {
            public string FK_Name { get; set; } = null!;
            public string FK_Schema { get; set; } = null!;
            public string FK_Table { get; set; } = null!;
            public string FK_Column { get; set; } = null!;
            public string PK_Name { get; set; } = null!;
            public string PK_Schema { get; set; } = null!;
            public string PK_Table { get; set; } = null!;
            public string PK_Column { get; set; } = null!;
        }
    }
}