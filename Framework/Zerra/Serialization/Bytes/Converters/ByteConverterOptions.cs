// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization
{
    [Flags]
    internal enum ByteConverterOptions : byte
    {
        None = 0,
        UsePropertyNames = 1,
        IncludePropertyTypes = 2,
        IgnoreIndexAttribute = 4,
        IndexSizeUInt16 = 8
    }
}