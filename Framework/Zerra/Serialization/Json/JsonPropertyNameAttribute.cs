// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    /// <summary>
    /// Specifies a custom name for a property or field during JSON serialization and deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class JsonPropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Gets the custom name to use for this property or field in JSON.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPropertyNameAttribute"/> class with the specified custom name.
        /// </summary>
        /// <param name="name">The custom name to use for this property or field when serializing to or deserializing from JSON.</param>
        public JsonPropertyNameAttribute(string name)
        {
            Name = name;
        }
    }
}