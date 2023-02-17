// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization
{
    public partial class ByteSerializer
    {
        private enum WriteFrameType
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