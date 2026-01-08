// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Bytes
{
    /// <summary>
    /// Options for configuring byte serialization and deserialization behavior.
    /// </summary>
    /// <remarks>Use this class to customize how objects are converted to and from byte arrays, including type handling,
    /// value handling, and error handling. These options affect both serialization and deserialization processes.</remarks>
    public sealed class ByteSerializerOptions
    {
        /// <summary>
        /// Use type information in the serialization. This allows the data to be deserialized without knowing the type beforehand and required for properties that are boxed or an interface. Default false.
        /// </summary>
        public bool UseTypes { get; set; } = false;
        /// <summary>
        /// Ignore the index attribute marked on object members. Default false.
        /// </summary>
        public bool IgnoreIndexAttribute { get; set; } = false;
        /// <summary>
        /// The type of index to be used for objects with members. Default Byte.
        /// </summary>
        public ByteSerializerIndexType IndexType { get; set; } = ByteSerializerIndexType.Byte;
    }
}