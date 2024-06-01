// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        private enum WriteFrameType : byte
        {
            Null,
            CoreType,
            EnumType,
            SpecialType,
            ByteArray,
            Object,

            CoreTypeEnumerable,
            EnumEnumerable,
            SpecialTypeEnumerable,
            Enumerable,
            ObjectEnumerable
        }
    }
}