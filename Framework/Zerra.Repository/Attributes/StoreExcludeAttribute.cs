// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Marks a property or field to be excluded from data store operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StoreExcludeAttribute : Attribute
    {
        /// <summary>Initializes a new instance of <see cref="StoreExcludeAttribute"/>.</summary>
        public StoreExcludeAttribute()
        {
        }
    }
}
