// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Reflection;

namespace Zerra.Repository
{
    /// <summary>
    /// Marks a class as a data store entity, optionally specifying its schema and store name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EntityAttribute : GenerateTypeDetailAttribute
    {
        /// <summary>Gets the schema in the data store, or <see langword="null"/> to use the data store's default.</summary>
        public string? Schema { get; }
        /// <summary>Gets the name used in the data store, or <see langword="null"/> to use the class name.</summary>
        public string? StoreName { get; }
        /// <summary>Gets the schema and store name as <c>{Schema}.{StoreName}</c>, or only the store name without a schema.</summary>
        public string? FullName { get; private set; }

        /// <summary>Initializes a new instance of <see cref="EntityAttribute"/> with an optional store name.</summary>
        /// <param name="storeName">The name to use in the data store. Must contain only letters, digits, underscores, or backticks. Pass <see langword="null"/> or empty to use the class name.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="storeName"/> contains invalid characters.</exception>
        public EntityAttribute(string? storeName = null)
        {
            if (!String.IsNullOrWhiteSpace(storeName))
            {
                if (!storeName.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(EntityAttribute)}.{nameof(StoreName)}={storeName}");
                this.StoreName = storeName;
            }
            this.FullName = this.StoreName;
        }

        /// <summary>Initializes a new instance of <see cref="EntityAttribute"/> with a schema and an optional store name.</summary>
        /// <param name="schema">The schema in the data store. Must contain only letters, digits, underscores, or backticks. Pass <see langword="null"/> or empty to use the data store's default.</param>
        /// <param name="storeName">The name to use in the data store. Must contain only letters, digits, underscores, or backticks. Pass <see langword="null"/> or empty to use the class name.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="schema"/> or <paramref name="storeName"/> contains invalid characters.</exception>
        public EntityAttribute(string? schema, string? storeName)
        {
            if (!String.IsNullOrWhiteSpace(schema))
            {
                if (!schema.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(EntityAttribute)}.{nameof(Schema)}={schema}");
                this.Schema = schema;
            }
            if (!String.IsNullOrWhiteSpace(storeName))
            {
                if (!storeName.All(x => Char.IsLetterOrDigit(x) || x == '_' || x == '`'))
                    throw new ArgumentException($"{nameof(EntityAttribute)}.{nameof(StoreName)}={storeName}");
                this.StoreName = storeName;
            }
            this.FullName = this.Schema is null ? this.StoreName : $"{this.Schema}.{this.StoreName}";
        }
    }
}
