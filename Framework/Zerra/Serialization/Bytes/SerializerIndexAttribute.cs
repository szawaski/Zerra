// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Bytes
{
    /// <summary>
    /// Specifies a manual index for a property or field during byte serialization to maintain consistent indexes when the object is changed.
    /// </summary>
    /// <remarks>
    /// Once this attribute is used on any member of an object, serialization will only include members that are decorated with this attribute.
    /// Each index must be unique within the type. This allows for backward compatibility when properties are added, removed, or reordered.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SerializerIndexAttribute : Attribute
    {
        /// <summary>
        /// Gets the index value for this property or field during serialization.
        /// </summary>
        public ushort Index { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerIndexAttribute"/> class with the specified index.
        /// </summary>
        /// <param name="index">The unique index value for this property or field during serialization. Must be unique among all indexed members in the type.</param>
        public SerializerIndexAttribute(ushort index)
        {
            Index = index;
        }
    }
}