// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    public sealed class JsonSerializerOptionsOld
    {
        /// <summary>
        /// A special feature to write JSON as arrays instead of property names. The exact same model is needed to deserialize.
        /// </summary>
        public bool Nameless { get; set; }
        /// <summary>
        /// Properties with null values will not be written.
        /// </summary>
        public bool DoNotWriteNullProperties { get; set; }
        /// <summary>
        /// Enums will serialize as their numeric value instead of the name string.
        /// </summary>
        public bool EnumAsNumber { get; set; }
    }
}