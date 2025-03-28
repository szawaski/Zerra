﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration
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
        DateOnly,
        TimeOnly,
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
        DateOnlyNullable,
        TimeOnlyNullable,
        GuidNullable,

        String
    }
}
