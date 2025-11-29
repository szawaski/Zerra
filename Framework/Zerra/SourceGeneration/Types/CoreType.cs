// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    public enum CoreType : byte
    {
        Boolean,
        Byte,
        SByte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal,
        Char,
        DateTime,
        DateTimeOffset,
        TimeSpan,
#if NET6_0_OR_GREATER
        DateOnly,
        TimeOnly,
#endif
        Guid,

        BooleanNullable,
        ByteNullable,
        SByteNullable,
        Int16Nullable,
        UInt16Nullable,
        Int32Nullable,
        UInt32Nullable,
        Int64Nullable,
        UInt64Nullable,
        SingleNullable,
        DoubleNullable,
        DecimalNullable,
        CharNullable,
        DateTimeNullable,
        DateTimeOffsetNullable,
        TimeSpanNullable,
#if NET6_0_OR_GREATER
        DateOnlyNullable,
        TimeOnlyNullable,
#endif
        GuidNullable,

        String
    }
}
