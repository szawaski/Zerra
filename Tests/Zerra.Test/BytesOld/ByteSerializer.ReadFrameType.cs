// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializerOld
    {
        private enum ReadFrameType : byte
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
}