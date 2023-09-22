// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;

namespace Zerra.Serialization
{
    public sealed class ByteSerializerOptions
    {
        /// <summary>
        /// Include property names in the serialization. Default false.
        /// </summary>
        public bool UsePropertyNames { get; set; } = false;
        /// <summary>
        /// Include type information for non-coretypes in the serialization. This allows the data to be deserialized without knowing the type beforehand and needed for properties that are boxed or an interface. Default false.
        /// </summary>
        public bool IncludePropertyTypes { get; set; } = false;
        /// <summary>
        /// Ignore the index attribute marked on properties. Default false.
        /// </summary>
        public bool IgnoreIndexAttribute { get; set; } = false;
        /// <summary>
        /// Size of the property indexes. Use Byte unless the number of properties in an object exceeds 255. Default Byte.
        /// </summary>
        public ByteSerializerIndexSize IndexSize { get; set; } = ByteSerializerIndexSize.Byte;
        /// <summary>
        /// The text encoder used for char and string.  Default System.Text.Encoding.UTF8.
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }
}