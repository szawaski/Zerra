// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Reflection
{
    public static class TypeLookup
    {
        private static readonly IReadOnlyDictionary<Type, CoreType> coreTypeLookup = new Dictionary<Type, CoreType>()
        {
            { typeof(bool), CoreType.Boolean },
            { typeof(byte), CoreType.Byte },
            { typeof(sbyte), CoreType.SByte },
            { typeof(short), CoreType.Int16 },
            { typeof(ushort), CoreType.UInt16 },
            { typeof(int), CoreType.Int32 },
            { typeof(uint), CoreType.UInt32 },
            { typeof(long), CoreType.Int64 },
            { typeof(ulong), CoreType.UInt64 },
            { typeof(float), CoreType.Single },
            { typeof(double), CoreType.Double },
            { typeof(decimal), CoreType.Decimal },
            { typeof(char), CoreType.Char },
            { typeof(DateTime), CoreType.DateTime },
            { typeof(DateTimeOffset), CoreType.DateTimeOffset },
            { typeof(TimeSpan), CoreType.TimeSpan },
#if NET6_0_OR_GREATER
            { typeof(DateOnly), CoreType.DateOnly },
            { typeof(TimeOnly), CoreType.TimeOnly },
#endif
            { typeof(Guid), CoreType.Guid },

            { typeof(string), CoreType.String },

            { typeof(bool?), CoreType.BooleanNullable },
            { typeof(byte?), CoreType.ByteNullable },
            { typeof(sbyte?), CoreType.SByteNullable },
            { typeof(short?), CoreType.Int16Nullable },
            { typeof(ushort?), CoreType.UInt16Nullable },
            { typeof(int?), CoreType.Int32Nullable },
            { typeof(uint?), CoreType.UInt32Nullable },
            { typeof(long?), CoreType.Int64Nullable },
            { typeof(ulong?), CoreType.UInt64Nullable },
            { typeof(float?), CoreType.SingleNullable },
            { typeof(double?), CoreType.DoubleNullable },
            { typeof(decimal?), CoreType.DecimalNullable },
            { typeof(char?), CoreType.CharNullable },
            { typeof(DateTime?), CoreType.DateTimeNullable },
            { typeof(DateTimeOffset?), CoreType.DateTimeOffsetNullable },
            { typeof(TimeSpan?), CoreType.TimeSpanNullable },
#if NET6_0_OR_GREATER
            { typeof(DateOnly?), CoreType.DateOnlyNullable },
            { typeof(TimeOnly?), CoreType.TimeOnlyNullable },
#endif
            { typeof(Guid?), CoreType.GuidNullable }
        };
        public static bool CoreTypeLookup(Type type, out CoreType coreType)
        {
            return coreTypeLookup.TryGetValue(type, out coreType);
        }

        private static readonly IReadOnlyDictionary<Type, CoreEnumType> coreEnumTypeLookup = new Dictionary<Type, CoreEnumType>()
        {
            { typeof(byte), CoreEnumType.Byte },
            { typeof(sbyte), CoreEnumType.SByte },
            { typeof(short), CoreEnumType.Int16 },
            { typeof(ushort), CoreEnumType.UInt16 },
            { typeof(int), CoreEnumType.Int32 },
            { typeof(uint), CoreEnumType.UInt32 },
            { typeof(long), CoreEnumType.Int64 },
            { typeof(ulong), CoreEnumType.UInt64 },
           
            { typeof(byte?), CoreEnumType.ByteNullable },
            { typeof(sbyte?), CoreEnumType.SByteNullable },
            { typeof(short?), CoreEnumType.Int16Nullable },
            { typeof(ushort?), CoreEnumType.UInt16Nullable },
            { typeof(int?), CoreEnumType.Int32Nullable },
            { typeof(uint?), CoreEnumType.UInt32Nullable },
            { typeof(long?), CoreEnumType.Int64Nullable },
            { typeof(ulong?), CoreEnumType.UInt64Nullable },
        };
        public static bool CoreEnumTypeLookup(Type type, out CoreEnumType coreType)
        {
            return coreEnumTypeLookup.TryGetValue(type, out coreType);
        }

        public static readonly IReadOnlyList<Type> CoreTypes = new Type[]
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(char),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
#if NET6_0_OR_GREATER
            typeof(DateOnly),
            typeof(TimeOnly),
#endif
            typeof(Guid),

            typeof(string),
        };

        public static readonly IReadOnlyList<Type> CoreTypesWithNullables = new Type[]
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(char),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
#if NET6_0_OR_GREATER
            typeof(DateOnly),
            typeof(TimeOnly),
#endif
            typeof(Guid),

            typeof(string),

            typeof(bool?),
            typeof(byte?),
            typeof(sbyte?),
            typeof(short?),
            typeof(ushort?),
            typeof(int?),
            typeof(uint?),
            typeof(long?),
            typeof(ulong?),
            typeof(float?),
            typeof(double?),
            typeof(decimal?),
            typeof(char?),
            typeof(DateTime?),
            typeof(DateTimeOffset?),
            typeof(TimeSpan?),
#if NET6_0_OR_GREATER
            typeof(DateOnly?),
            typeof(TimeOnly?),
#endif
            typeof(Guid?)
        };

        public static readonly IReadOnlyList<Type> CoreTypeNumbers = new Type[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        public static readonly IReadOnlyList<Type> CoreTypeNumbersWithNullables = new Type[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),

            typeof(byte?),
            typeof(sbyte?),
            typeof(short?),
            typeof(ushort?),
            typeof(int?),
            typeof(uint?),
            typeof(long?),
            typeof(ulong?),
            typeof(float?),
            typeof(double?),
            typeof(decimal?)
        };

        public static readonly IReadOnlyList<CoreType> CoreTypeNumberEnums = new CoreType[]
        {
            CoreType.Byte,
            CoreType.SByte,
            CoreType.Int16,
            CoreType.UInt16,
            CoreType.Int32,
            CoreType.UInt32,
            CoreType.Int64,
            CoreType.UInt64,
            CoreType.Single,
            CoreType.Double,
            CoreType.Decimal
        };

        public static readonly IReadOnlyList<CoreType> CoreTypeNumberEnumsWithNullables = new CoreType[]
        {
            CoreType.Byte,
            CoreType.SByte,
            CoreType.Int16,
            CoreType.UInt16,
            CoreType.Int32,
            CoreType.UInt32,
            CoreType.Int64,
            CoreType.UInt64,
            CoreType.Single,
            CoreType.Double,
            CoreType.Decimal,

            CoreType.ByteNullable,
            CoreType.SByteNullable,
            CoreType.Int16Nullable,
            CoreType.UInt16Nullable,
            CoreType.Int32Nullable,
            CoreType.UInt32Nullable,
            CoreType.Int64Nullable,
            CoreType.UInt64Nullable,
            CoreType.SingleNullable,
            CoreType.DoubleNullable,
            CoreType.DecimalNullable
        };

        public static readonly IReadOnlyList<Type> SpecialTypes = new Type[]
        {
            typeof(Type),
            typeof(Dictionary<,>),
        };

        public static bool SpecialTypeLookup(Type type, out SpecialType specialType)
        {
            switch (type.Name)
            {
                case "Task":
                case "Task`1":
                    specialType = SpecialType.Task;
                    return true;
                case "Type":
                case "RuntimeType":
                    specialType = SpecialType.Type;
                    return true;
                case "IDictionary`2":
                case "IReadOnlyDictionary`2":
                case "Dictionary`2":
                case "ConcurrentDictionary`2":
                case "ConcurrentFactoryDictionary`2":
                    specialType = SpecialType.Dictionary;
                    return true;
                default:
                    specialType = default;
                    return false;
            }
        }
    }
}
