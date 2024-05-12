// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    internal enum WriteFrameType : byte
    {
        PropertyType,

        CoreType,
        EnumType,
        SpecialType,
        Object,

        CoreTypeEnumerable,
        EnumTypeEnumerable,
        //SpecialTypeEnumerable,
        ObjectEnumerable,
    }
}