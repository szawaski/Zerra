// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.MySql
{
    public sealed partial class MySqlEngine
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