// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.MsSql
{
    public sealed partial class MsSqlEngine
    {
        private sealed class SqlColumnType
        {
            public string Table { get; set; } = null!;
            public string Column { get; set; } = null!;
            public string DataType { get; set; } = null!;
            public bool IsNullable { get; set; }
            public int? CharacterMaximumLength { get; set; }
            public byte? NumericPrecision { get; set; }
            public int? NumericScale { get; set; }
            public short? DatetimePrecision { get; set; }
            public bool IsIdentity { get; set; }
            public bool IsPrimaryKey { get; set; }
        }
    }
}