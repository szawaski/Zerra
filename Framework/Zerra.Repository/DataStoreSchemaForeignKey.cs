// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// A foreign key read from a data store.
    /// </summary>
    public sealed class DataStoreSchemaForeignKey
    {
        /// <summary>The constraint name in the data store.</summary>
        public string Name { get; }
        /// <summary>The columns of the table holding the key, in the key's order.</summary>
        public IReadOnlyList<string> Columns { get; }
        /// <summary>The table the key refers to.</summary>
        public string ReferencedTable { get; }
        /// <summary>The columns referred to, in the key's order.</summary>
        public IReadOnlyList<string> ReferencedColumns { get; }

        /// <summary>
        /// Creates a foreign key.
        /// </summary>
        /// <param name="name">The constraint name in the data store.</param>
        /// <param name="columns">The columns of the table holding the key, in the key's order.</param>
        /// <param name="referencedTable">The table the key refers to.</param>
        /// <param name="referencedColumns">The columns referred to, in the key's order.</param>
        public DataStoreSchemaForeignKey(string name, IReadOnlyList<string> columns, string referencedTable, IReadOnlyList<string> referencedColumns)
        {
            this.Name = name;
            this.Columns = columns;
            this.ReferencedTable = referencedTable;
            this.ReferencedColumns = referencedColumns;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
