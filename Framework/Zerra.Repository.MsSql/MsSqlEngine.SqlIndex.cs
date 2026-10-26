// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.MsSql
{
    public sealed partial class MsSqlEngine
    {
        private sealed class SqlIndex
        {
            public string Name { get; set; } = null!;
            public bool IsUnique { get; set; }
            public bool IsClustered { get; set; }
            public string? FilterDefinition { get; set; }
            public List<string> Columns { get; set; } = null!;
            public List<bool> Descending { get; set; } = null!;
        }
    }
}