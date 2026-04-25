// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Specifies the name used when storing a property or field in the data store.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class StoreNameAttribute : Attribute
    {
        /// <summary>Gets the name used in the data store.</summary>
        public string StoreName { get; }

        /// <summary>Initializes a new instance of <see cref="StoreNameAttribute"/> with the specified store name.</summary>
        /// <param name="storeName">The name to use in the data store. Must contain only letters, digits, underscores, or backticks.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="storeName"/> contains invalid characters.</exception>
        public StoreNameAttribute(string storeName)
        {
            if (!storeName.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                throw new ArgumentException($"{nameof(StoreNameAttribute)}.{nameof(StoreName)}={storeName}");
            this.StoreName = storeName;
        }
    }
}
