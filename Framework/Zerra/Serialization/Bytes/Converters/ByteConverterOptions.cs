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
        IncludePropertyTypes = 1
    }
}