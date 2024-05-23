// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    internal enum ReadFrameType : byte
    {
        PropertyType,

        CoreType,
        EnumType,
        SpecialType,
        Object,
        ObjectProperty,

        CoreTypeEnumerable,
        EnumTypeEnumerable,
        //SpecialTypeEnumerable,
        ObjectEnumerable,
    }
}