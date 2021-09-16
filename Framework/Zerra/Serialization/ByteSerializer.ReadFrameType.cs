// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public partial class ByteSerializer
    {
        private enum ReadFrameType
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