// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializerOld
    {
        private enum WriteFrameType : byte
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
}