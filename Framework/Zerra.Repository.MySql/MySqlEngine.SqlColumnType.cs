// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.MySql
{
    public sealed partial class MySqlEngine
    {
        private sealed class SqlColumnType
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
            public long? CharacterMaximumLength { get; set; }
            public uint? NumericPrecision { get; set; }
            public uint? NumericScale { get; set; }
            public uint? DatetimePrecision { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
    }
}