// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.PostgreSql
{
    public sealed partial class PostgreSqlEngine
    {
        private class SqlColumnType
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
            public int? CharacterMaximumLength { get; set; }
            public int? NumericPrecision { get; set; }
            public int? NumericScale { get; set; }
            public int? DatetimePrecision { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
    }
}