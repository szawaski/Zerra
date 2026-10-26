// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository
{
    /// <summary>
    /// Adds the property or field to an index in the data store.
    /// Members with the same <see cref="Name"/> are combined into one index.
    /// Indexed strings and byte arrays need a length from <see cref="StorePropertiesAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class StoreIndexAttribute : Attribute
    {
        /// <summary>Gets the name that groups members into one index, or <see langword="null"/> for an index on this member alone. The store name is IX_{Entity}_{Name}.</summary>
        public string? Name { get; }
        /// <summary>Gets or sets a value indicating whether the index is unique. Rows with a null in the index do not conflict.</summary>
        public bool Unique { get; set; }
        /// <summary>Gets or sets the position of this member in a combined index. Members with the same order keep their declared order.</summary>
        public int Order { get; set; }
        /// <summary>Gets or sets a value indicating whether this member is sorted descending in the index.</summary>
        public bool Descending { get; set; }

        /// <summary>Initializes a new instance of <see cref="StoreIndexAttribute"/> for an index on this member alone.</summary>
        public StoreIndexAttribute()
        {
        }

        /// <summary>Initializes a new instance of <see cref="StoreIndexAttribute"/> for the named index.</summary>
        /// <param name="name">The name that groups members into one index. Must contain only letters, digits, or underscores.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or contains invalid characters.</exception>
        public StoreIndexAttribute(string name)
        {
            if (String.IsNullOrWhiteSpace(name) || !name.All(x => Char.IsLetterOrDigit(x) || x == '_'))
                throw new ArgumentException($"{nameof(StoreIndexAttribute)}.{nameof(Name)}={name}");
            this.Name = name;
        }
    }
}