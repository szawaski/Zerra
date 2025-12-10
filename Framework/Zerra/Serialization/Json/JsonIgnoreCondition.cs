// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    /// <summary>
    /// Specifies the condition under which a property should be ignored during JSON serialization or deserialization.
    /// </summary>
    public enum JsonIgnoreCondition : byte
    {
        /// <summary>
        /// The property is never ignored and is always included in serialization and deserialization operations.
        /// </summary>
        Never = 0,
        
        /// <summary>
        /// The property is always ignored and excluded from both serialization and deserialization operations.
        /// </summary>
        Always = 1,
        
        /// <summary>
        /// The property is ignored only during deserialization (reading from JSON).
        /// </summary>
        WhenReading = 2,
        
        /// <summary>
        /// The property is ignored only during serialization (writing to JSON).
        /// </summary>
        WhenWriting = 3,
        
        /// <summary>
        /// The property is ignored during serialization when its value equals the default value for its type.
        /// </summary>
        WhenWritingDefault = 4,
        
        /// <summary>
        /// The property is ignored during serialization when its value is <c>null</c>.
        /// </summary>
        WhenWritingNull = 5
    }
}