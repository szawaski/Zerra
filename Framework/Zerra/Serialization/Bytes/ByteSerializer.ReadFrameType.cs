// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public sealed partial class ByteSerializer
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