// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Test
{
    public class SqlDialectExpectations
    {
        public string TableQuoteOpen { get; init; } = "[";
        public string TableQuoteClose { get; init; } = "]";
        public string ColumnQuoteOpen { get; init; } = "[";
        public string ColumnQuoteClose { get; init; } = "]";
        public bool FromSpaceBeforeQuote { get; init; } = false;
        public bool LowercaseIdentifiers { get; init; } = false;
        public string StringPrefix { get; init; } = "N";
        public bool HasTopClause { get; init; } = true;
        public bool HasLimitClause { get; init; } = false;
        public bool HasOffsetFetch { get; init; } = true;
        public bool HasLimitOffset { get; init; } = false;

        public string Table(string name)
        {
            var n = LowercaseIdentifiers ? name.ToLower() : name;
            if (string.IsNullOrEmpty(TableQuoteOpen))
                return n;
            return $"{TableQuoteOpen}{n}{TableQuoteClose}";
        }

        public string Column(string table, string column)
        {
            var t = LowercaseIdentifiers ? table.ToLower() : table;
            var c = LowercaseIdentifiers ? column.ToLower() : column;
            if (string.IsNullOrEmpty(ColumnQuoteOpen))
                return $"{t}.{c}";
            return $"{ColumnQuoteOpen}{t}{ColumnQuoteClose}.{ColumnQuoteOpen}{c}{ColumnQuoteClose}";
        }

        public string From(string name)
        {
            var space = FromSpaceBeforeQuote ? " " : "";
            return $"FROM{space}{Table(name)}";
        }
    }
}
