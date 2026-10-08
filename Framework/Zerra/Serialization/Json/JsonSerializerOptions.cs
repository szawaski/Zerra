// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    /// <summary>
    /// Provides options to control the behavior of JSON serialization and deserialization.
    /// </summary>
    /// <remarks>Use this class to customize how objects are converted to and from JSON, including property
    /// naming, value handling, enum representation, error handling, and case sensitivity. These options affect both
    /// serialization and deserialization processes. Changing certain options, such as enabling case-insensitive
    /// property matching, may impact performance.</remarks>
    public sealed class JsonSerializerOptions
    {
        /// <summary>
        /// A special feature to write JSON objects as arrays instead of property names. The exact same model is needed to deserialize.
        /// </summary>
        public bool Nameless { get; init; }
        /// <summary>
        /// Properties with null values will not be written.
        /// </summary>
        public bool DoNotWriteNullProperties { get; init; }
        /// <summary>
        /// Properties with default values will not be written.
        /// </summary>
        public bool DoNotWriteDefaultProperties { get; init; }
        /// <summary>
        /// Enums will serialize as their numeric value instead of the name string.
        /// </summary>
        public bool EnumAsNumber { get; init; }
        /// <summary>
        /// Throws when valid JSON doesn't fit the type, such as <c>1.5</c> for an <c>int</c>, an unparseable date, or an unknown enum name. When off, the default, the type's default value is used instead. Invalid JSON always throws.
        /// </summary>
        public bool ErrorOnReadMismatchedData { get; init; }
        /// <summary>
        /// Object members do not need match the case of the JSON when reading. This performs more slowly.
        /// </summary>
        public bool IgnoreCase { get; init; }
    }
}