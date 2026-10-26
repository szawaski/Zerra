// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.PostgreSql
{
    public sealed partial class PostgreSqlEngine
    {
        private sealed class SqlIndex
        {
            public string Name { get; set; } = null!;
            public bool IsUnique { get; set; }
            public List<string> Columns { get; set; } = null!;
            public List<bool> Descending { get; set; } = null!;
        }
    }
}