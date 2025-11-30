// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.SourceGeneration.Types
{
    /// <summary>
    /// Provides type lookup and classification utilities for core types, numeric types, enum types, and special framework types.
    /// Enables efficient categorization and routing of types during code generation and runtime type analysis.
    /// </summary>
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
        
        /// <summary>
        /// Attempts to look up a type in the core type catalog and retrieve its <see cref="CoreType"/> classification.
        /// </summary>
        /// <param name="type">The type to classify.</param>
        /// <param name="coreType">The core type classification if found; otherwise the default value.</param>
        /// <returns>True if the type is a recognized core type; otherwise false.</returns>
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
        
        /// <summary>
        /// Attempts to look up a type in the enum type catalog and retrieve its <see cref="CoreEnumType"/> classification.
        /// Only integral types and their nullable equivalents are recognized as valid enum underlying types.
        /// </summary>
        /// <param name="type">The type to classify.</param>
        /// <param name="coreType">The enum core type classification if found; otherwise the default value.</param>
        /// <returns>True if the type is a recognized enum underlying type; otherwise false.</returns>
        public static bool CoreEnumTypeLookup(Type type, out CoreEnumType coreType)
        {
            return coreEnumTypeLookup.TryGetValue(type, out coreType);
        }

        /// <summary>
        /// Attempts to classify a type as a special framework type.
        /// Recognizes Task variants, Type, Dictionary variants, and other framework infrastructure types.
        /// </summary>
        /// <param name="type">The type to classify.</param>
        /// <param name="specialType">The special type classification if recognized; otherwise the default value.</param>
        /// <returns>True if the type is recognized as a special type; otherwise false.</returns>
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
