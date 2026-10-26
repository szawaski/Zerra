// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.Reflection
{
    /// <summary>
    /// Holds metadata about an index declared with <see cref="StoreIndexAttribute"/> on a repository model type.
    /// </summary>
    public sealed class ModelIndexDetail
    {
        /// <summary>
        /// The index name in the data store, IX_{Entity}_{Name} or IX_{Entity}_{Column} when no name is given.
        /// </summary>
        public string StoreName { get; }
        /// <summary>
        /// Gets a value indicating whether the index is unique.
        /// </summary>
        public bool IsUnique { get; }
        /// <summary>
        /// The members in the index, in index order.
        /// </summary>
        public IReadOnlyList<ModelMemberDetail> Members { get; }
        /// <summary>
        /// Whether each member in <see cref="Members"/> is sorted descending.
        /// </summary>
        public IReadOnlyList<bool> Descending { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return StoreName;
        }

        internal ModelIndexDetail(string storeName, bool isUnique, ModelMemberDetail[] members, bool[] descending)
        {
            this.StoreName = storeName;
            this.IsUnique = isUnique;
            this.Members = members;
            this.Descending = descending;
        }
    }
}