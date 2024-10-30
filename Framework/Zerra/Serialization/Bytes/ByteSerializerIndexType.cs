// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Bytes
{
    /// <summary>
    /// The type of the index used when serializing object members. For exceptionally large objects, the Byte index size may be too small.
    /// </summary>
    public enum ByteSerializerIndexType : byte
    {
        /// <summary>
        /// Allows for 254 members to be serialized in a single object. Fastest and smallest data size. 
        /// Use <see cref="SerializerIndexAttribute" /> on members to specify the unique index value, otherwise the member order will be used.
        /// </summary>
        Byte,
        /// <summary>
        /// Allows for 65,534 members to be serialized in a single object.
        /// Use <see cref="SerializerIndexAttribute" /> on members to specify the unique index value, otherwise the member order will be used.
        /// </summary>
        UInt16,
        /// <summary>
        /// Use member names instead of an index. Slowest and largest data size but the most versatile.
        /// </summary>
        MemberNames,
    }
}