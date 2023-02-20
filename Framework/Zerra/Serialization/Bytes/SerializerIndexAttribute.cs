// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    public class SerializerIndexAttribute : Attribute
    {
        public ushort Index { get; private set; }
        public SerializerIndexAttribute(ushort index)
        {
            this.Index = index;
        }
    }
}