// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Reflection
{
    public enum CoreType : byte
    {
        Boolean,
        Byte,
        SByte,
        UInt16,
        Int16,
        UInt32,
        Int32,
        UInt64,
        Int64,
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
        UInt16Nullable,
        Int16Nullable,
        UInt32Nullable,
        Int32Nullable,
        UInt64Nullable,
        Int64Nullable,
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
