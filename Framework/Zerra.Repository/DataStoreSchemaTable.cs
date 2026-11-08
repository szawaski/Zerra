// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// A table read from a data store by <see cref="ITransactStoreEngine.ReadDataStoreSchema"/>, described the way a model would declare it.
    /// </summary>
    public sealed class DataStoreSchemaTable
    {
        /// <summary>The table name in the data store.</summary>
        public string Name { get; }
        /// <summary>The columns in the data store's order.</summary>
        public IReadOnlyList<DataStoreSchemaColumn> Columns { get; }
        /// <summary>The foreign keys this table holds.</summary>
        public IReadOnlyList<DataStoreSchemaForeignKey> ForeignKeys { get; }
        /// <summary>The indexes other than the primary key and the ones the data store keeps for itself.</summary>
        public IReadOnlyList<DataStoreSchemaIndex> Indexes { get; }

        /// <summary>
        /// Creates a table.
        /// </summary>
        /// <param name="name">The table name in the data store.</param>
        /// <param name="columns">The columns in the data store's order.</param>
        /// <param name="foreignKeys">The foreign keys this table holds.</param>
        /// <param name="indexes">The indexes other than the primary key and the ones the data store keeps for itself.</param>
        public DataStoreSchemaTable(string name, IReadOnlyList<DataStoreSchemaColumn> columns, IReadOnlyList<DataStoreSchemaForeignKey> foreignKeys, IReadOnlyList<DataStoreSchemaIndex> indexes)
        {
            this.Name = name;
            this.Columns = columns;
            this.ForeignKeys = foreignKeys;
            this.Indexes = indexes;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
