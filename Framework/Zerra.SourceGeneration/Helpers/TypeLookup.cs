// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration
{
    public static class TypeLookup
    {
        private static readonly IReadOnlyDictionary<string, CoreType> coreTypeLookup = new Dictionary<string, CoreType>()
        {
            { "bool", CoreType.Boolean },
            { "byte", CoreType.Byte },
            { "sbyte", CoreType.SByte },
            { "short", CoreType.Int16 },
            { "ushort", CoreType.UInt16 },
            { "int", CoreType.Int32 },
            { "uint", CoreType.UInt32 },
            { "long", CoreType.Int64 },
            { "ulong", CoreType.UInt64 },
            { "float", CoreType.Single },
            { "double", CoreType.Double },
            { "decimal", CoreType.Decimal },
            { "char", CoreType.Char },

            { "Boolean", CoreType.Boolean },
            { "Byte", CoreType.Byte },
            { "SByte", CoreType.SByte },
            { "Int16", CoreType.Int16 },
            { "UInt16", CoreType.UInt16 },
            { "Int32", CoreType.Int32 },
            { "UInt32", CoreType.UInt32 },
            { "Int64", CoreType.Int64 },
            { "UInt64", CoreType.UInt64 },
            { "Single", CoreType.Single },
            { "Double", CoreType.Double },
            { "Decimal", CoreType.Decimal },
            { "Char", CoreType.Char },

            { "DateTime", CoreType.DateTime },
            { "DateTimeOffset", CoreType.DateTimeOffset },
            { "TimeSpan", CoreType.TimeSpan },

            { "DateOnly", CoreType.DateOnly },
            { "TimeOnly", CoreType.TimeOnly },

            { "Guid", CoreType.Guid },

            { "string", CoreType.String },
            { "String", CoreType.String },

            { "bool?", CoreType.BooleanNullable },
            { "byte?", CoreType.ByteNullable },
            { "sbyte?", CoreType.SByteNullable },
            { "short?", CoreType.Int16Nullable },
            { "ushort?", CoreType.UInt16Nullable },
            { "int?", CoreType.Int32Nullable },
            { "uint?", CoreType.UInt32Nullable },
            { "long?", CoreType.Int64Nullable },
            { "ulong?", CoreType.UInt64Nullable },
            { "float?", CoreType.SingleNullable },
            { "double?", CoreType.DoubleNullable },
            { "decimal?", CoreType.DecimalNullable },
            { "char?", CoreType.CharNullable },

            { "Boolean?", CoreType.BooleanNullable },
            { "Byte?", CoreType.ByteNullable },
            { "SByte?", CoreType.SByteNullable },
            { "Int16?", CoreType.Int16Nullable },
            { "UInt16?", CoreType.UInt16Nullable },
            { "Int32?", CoreType.Int32Nullable },
            { "UInt32?", CoreType.UInt32Nullable },
            { "Int64?", CoreType.Int64Nullable },
            { "UInt64?", CoreType.UInt64Nullable },
            { "Single?", CoreType.SingleNullable },
            { "Double?", CoreType.DoubleNullable },
            { "Decimal?", CoreType.DecimalNullable },
            { "Char?", CoreType.CharNullable },

            { "DateTime?", CoreType.DateTimeNullable },
            { "DateTimeOffset?", CoreType.DateTimeOffsetNullable },
            { "TimeSpan?", CoreType.TimeSpanNullable },

            { "DateOnly?", CoreType.DateOnlyNullable },
            { "TimeOnly?", CoreType.TimeOnlyNullable },

            { "Guid?", CoreType.GuidNullable }
        };
        public static bool CoreTypeLookup(string type, out CoreType coreType)
        {
            type = type.Split('.').Last();
            return coreTypeLookup.TryGetValue(type, out coreType);
        }
        public static IEnumerable<string> GetCoreTypeNames => coreTypeLookup.Keys;

        private static readonly IReadOnlyDictionary<string, CoreEnumType> coreEnumTypeLookup = new Dictionary<string, CoreEnumType>()
        {
            { "byte", CoreEnumType.Byte },
            { "sbyte", CoreEnumType.SByte },
            { "short", CoreEnumType.Int16 },
            { "ushort", CoreEnumType.UInt16 },
            { "int", CoreEnumType.Int32 },
            { "uint", CoreEnumType.UInt32 },
            { "long", CoreEnumType.Int64 },
            { "ulong", CoreEnumType.UInt64 },

            { "byte?", CoreEnumType.ByteNullable },
            { "sbyte?", CoreEnumType.SByteNullable },
            { "short?", CoreEnumType.Int16Nullable },
            { "ushort?", CoreEnumType.UInt16Nullable },
            { "int?", CoreEnumType.Int32Nullable },
            { "uint?", CoreEnumType.UInt32Nullable },
            { "long?", CoreEnumType.Int64Nullable },
            { "ulong?", CoreEnumType.UInt64Nullable },

            { "Byte", CoreEnumType.Byte },
            { "SByte", CoreEnumType.SByte },
            { "Int16", CoreEnumType.Int16 },
            { "UInt16", CoreEnumType.UInt16 },
            { "Int32", CoreEnumType.Int32 },
            { "UInt32", CoreEnumType.UInt32 },
            { "Int64", CoreEnumType.Int64 },
            { "UInt64", CoreEnumType.UInt64 },

            { "Byte?", CoreEnumType.ByteNullable },
            { "SByte?", CoreEnumType.SByteNullable },
            { "Int16?", CoreEnumType.Int16Nullable },
            { "UInt16?", CoreEnumType.UInt16Nullable },
            { "Int32?", CoreEnumType.Int32Nullable },
            { "UInt32?", CoreEnumType.UInt32Nullable },
            { "Int64?", CoreEnumType.Int64Nullable },
            { "UInt64?", CoreEnumType.UInt64Nullable },
        };
        public static bool CoreEnumTypeLookup(string type, out CoreEnumType coreType)
        {
            type = type.Split('.').Last();
            return coreEnumTypeLookup.TryGetValue(type, out coreType);
        }
        public static bool SpecialTypeLookup(string type, out SpecialType specialType)
        {
            type = type.Split('<').First().Split('.').Last();
            switch (type)
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
                case "object":
                case "Object":
                    specialType = SpecialType.Object;
                    return true;
                case "void":
                    specialType = SpecialType.Void;
                    return true;
                case "IntPtr":
                case "nint":
                case "UIntPtr":
                case "unint":
                    specialType = SpecialType.Pointer;
                    return true;
                case "CancellationToken":
                    specialType = SpecialType.CancellationToken;
                    return true;
                default:
                    specialType = default;
                    return false;
            }
        }
    }
}
