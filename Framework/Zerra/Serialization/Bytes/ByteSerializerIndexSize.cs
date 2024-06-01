// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Bytes
{
    /// <summary>
    /// The size of the index used when serializing properties without names.  For exceptionally large objects, the index size may need increased.
    /// </summary>
    public enum ByteSerializerIndexSize : byte
    {
        /// <summary>
        /// Allows for 254 members to be serialized in a single object.
        /// </summary>
        Byte,
        /// <summary>
        /// Allows for 65,534 members to be serialized in a single object.
        /// </summary>
        UInt16,
    }
}