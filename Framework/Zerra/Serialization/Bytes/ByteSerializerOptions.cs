// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Serialization.Bytes
{
    public sealed class ByteSerializerOptions
    {
        /// <summary>
        /// Include property names in the serialization. Default false.
        /// </summary>
        public bool UsePropertyNames { get; set; } = false;
        /// <summary>
        /// Use type information in the serialization. This allows the data to be deserialized without knowing the type beforehand and required for properties that are boxed or an interface. Default false.
        /// </summary>
        public bool UseTypes { get; set; } = false;
        /// <summary>
        /// Ignore the index attribute marked on properties. Default false.
        /// </summary>
        public bool IgnoreIndexAttribute { get; set; } = false;
        /// <summary>
        /// Size of the property indexes when member names are not used. Use Byte unless the number of properties in an object exceeds 254. Default Byte.
        /// </summary>
        public ByteSerializerIndexSize IndexSize { get; set; } = ByteSerializerIndexSize.Byte;
        /// <summary>
        /// The text encoder used for string.  Default System.Text.Encoding.UTF8.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }
}