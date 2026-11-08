// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// An index read from a data store.
    /// </summary>
    public sealed class DataStoreSchemaIndex
    {
        /// <summary>The index name in the data store.</summary>
        public string Name { get; }
        /// <summary>The index only allows one row for each value.</summary>
        public bool IsUnique { get; }
        /// <summary>The columns in the index's order.</summary>
        public IReadOnlyList<string> Columns { get; }
        /// <summary>Whether each column is sorted descending.</summary>
        public IReadOnlyList<bool> Descending { get; }

        /// <summary>
        /// Creates an index.
        /// </summary>
        /// <param name="name">The index name in the data store.</param>
        /// <param name="isUnique">The index only allows one row for each value.</param>
        /// <param name="columns">The columns in the index's order.</param>
        /// <param name="descending">Whether each column is sorted descending.</param>
        public DataStoreSchemaIndex(string name, bool isUnique, IReadOnlyList<string> columns, IReadOnlyList<bool> descending)
        {
            this.Name = name;
            this.IsUnique = isUnique;
            this.Columns = columns;
            this.Descending = descending;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
