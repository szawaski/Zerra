// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization.Bytes
{
    /// <summary>
    /// Manually set the property index for serialization to keep the indexes the same with the object is changed.
    /// Once one attribute is used on an object member, serialization will only include members with the attribute.
    /// Each index must be unique.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class SerializerIndexAttribute : Attribute
    {
        public ushort Index { get; }
        public SerializerIndexAttribute(ushort index)
        {
            Index = index;
        }
    }
}