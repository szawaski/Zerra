// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.Reflection.Dynamic;

namespace Zerra.Reflection
{
    /// <summary>
    /// Provides runtime type analysis and conversion services for the Zerra framework.
    /// Generates and caches detailed type information for reflection-based serialization, 
    /// deserialization, and CQRS message routing.
    /// </summary>
    public static class TypeAnalyzer
    {
        private static readonly ConcurrentFactoryDictionary<Type, TypeDetail> byType = new();

        /// <summary>
        /// Gets or generates detailed type information for the specified type.
        /// If type detail was source generated, it will be retrieved from cache without throwing.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>Cached or newly generated type detail information.</returns>
        /// <exception cref="NotSupportedException">Thrown if dynamic code generation is required but not supported in the current build configuration.</exception>
        public static TypeDetail GetTypeDetail(Type type)
        {
            return byType.GetOrAdd(type, GenerateTypeDetail);
        }

        /// <summary>
        /// Registers pre-generated type information in the analyzer cache.
        /// </summary>
        /// <remarks>
        /// Used by source generators to register type details without requiring runtime type generation.
        /// Core types may appear in multiple assemblies, so duplicate registration is allowed.
        /// </remarks>
        /// <param name="typeInfo">The type detail information to register.</param>
        internal static void Register(TypeDetail typeInfo)
        {
            //core types might repeat in different assemblies so don't duplicate check
            _ = byType.TryAdd(typeInfo.Type, typeInfo);
        }

        private static TypeDetail GenerateTypeDetail(Type type)
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                throw new NotSupportedException($"Cannot generate type detail for {type.Name}. Dynamic code generation is not supported in this build configuration.");

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            return TypeDetailGenerator.GenerateTypeDetail(type);
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
        }

        /// <summary>
        /// Converts an object to the specified type.
        /// Supports all core types (primitives, dates, times, guids, strings) and their nullable equivalents.
        /// </summary>
        /// <typeparam name="T">The target type to convert to.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The converted value, or null if conversion fails or type is not supported.</returns>
        /// <exception cref="NotImplementedException">Thrown if the target type is not supported for conversion.</exception>
        public static T? Convert<T>(object? obj)
        {
            var type = typeof(T);
            if (!TypeLookup.GetCoreType(type, out var coreType))
                throw new NotImplementedException($"Type convert not available for {type.Name}");
            return (T?)Convert(obj, coreType);
        }

        /// <summary>
        /// Converts an object to the specified type.
        /// Supports all core types (primitives, dates, times, guids, strings) and their nullable equivalents.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="type">The target type to convert to.</param>
        /// <returns>The converted value, or null if conversion fails or type is not supported.</returns>
        /// <exception cref="NotImplementedException">Thrown if the target type is not supported for conversion.</exception>
        public static object? Convert(object? obj, Type type)
        {
            if (!TypeLookup.GetCoreType(type, out var coreType))
                throw new NotImplementedException($"Type convert not available for {type.Name}");
            return Convert(obj, coreType);
        }

        /// <summary>
        /// Converts an object to the specified core type.
        /// Supports all core types (primitives, dates, times, guids, strings) and their nullable equivalents.
        /// </summary>
        /// <remarks>
        /// This overload accepts pre-resolved CoreType enum for performance optimization.
        /// Returns default values for null inputs on non-nullable types and null for null inputs on nullable types.
        /// </remarks>
        /// <param name="obj">The object to convert.</param>
        /// <param name="coreType">The target core type to convert to.</param>
        /// <returns>The converted value, or default/null if conversion fails or type is not supported.</returns>
        /// <exception cref="NotImplementedException">Thrown if the target core type is not supported for conversion.</exception>
        public static object? Convert(object? obj, CoreType coreType)
        {
            if (obj is null)
            {
                return coreType switch
                {
                    CoreType.Boolean => default(bool),
                    CoreType.Byte => default(byte),
                    CoreType.SByte => default(sbyte),
                    CoreType.UInt16 => default(ushort),
                    CoreType.Int16 => default(short),
                    CoreType.UInt32 => default(uint),
                    CoreType.Int32 => default(int),
                    CoreType.UInt64 => default(ulong),
                    CoreType.Int64 => default(long),
                    CoreType.Single => default(float),
                    CoreType.Double => default(double),
                    CoreType.Decimal => default(decimal),
                    CoreType.Char => default(char),
                    CoreType.DateTime => default(DateTime),
                    CoreType.DateTimeOffset => default(DateTimeOffset),
                    CoreType.TimeSpan => default(TimeSpan),
#if NET6_0_OR_GREATER
                    CoreType.DateOnly => default(DateOnly),
                    CoreType.TimeOnly => default(TimeOnly),
#endif
                    CoreType.Guid => default(Guid),
                    CoreType.String => default(string),
                    CoreType.BooleanNullable => null,
                    CoreType.ByteNullable => null,
                    CoreType.SByteNullable => null,
                    CoreType.UInt16Nullable => null,
                    CoreType.Int16Nullable => null,
                    CoreType.UInt32Nullable => null,
                    CoreType.Int32Nullable => null,
                    CoreType.UInt64Nullable => null,
                    CoreType.Int64Nullable => null,
                    CoreType.SingleNullable => null,
                    CoreType.DoubleNullable => null,
                    CoreType.DecimalNullable => null,
                    CoreType.CharNullable => null,
                    CoreType.DateTimeNullable => null,
                    CoreType.DateTimeOffsetNullable => null,
                    CoreType.TimeSpanNullable => null,
#if NET6_0_OR_GREATER
                    CoreType.DateOnlyNullable => null,
                    CoreType.TimeOnlyNullable => null,
#endif
                    CoreType.GuidNullable => null,
                    _ => throw new NotImplementedException($"Type conversion not available for {coreType}"),
                };
            }
            else
            {
                return coreType switch
                {
                    CoreType.Boolean => System.Convert.ToBoolean(obj),
                    CoreType.Byte => System.Convert.ToByte(obj),
                    CoreType.SByte => System.Convert.ToSByte(obj),
                    CoreType.UInt16 => System.Convert.ToUInt16(obj),
                    CoreType.Int16 => System.Convert.ToInt16(obj),
                    CoreType.UInt32 => System.Convert.ToUInt32(obj),
                    CoreType.Int32 => System.Convert.ToInt32(obj),
                    CoreType.UInt64 => System.Convert.ToUInt64(obj),
                    CoreType.Int64 => System.Convert.ToInt64(obj),
                    CoreType.Single => System.Convert.ToSingle(obj),
                    CoreType.Double => System.Convert.ToDouble(obj),
                    CoreType.Decimal => System.Convert.ToDecimal(obj),
                    CoreType.Char => System.Convert.ToChar(obj),
                    CoreType.DateTime => System.Convert.ToDateTime(obj),
                    CoreType.DateTimeOffset => ConvertToDateTimeOffset(obj),
                    CoreType.TimeSpan => ConvertToTimeSpan(obj),
#if NET6_0_OR_GREATER
                    CoreType.DateOnly => ConvertToDateOnly(obj),
                    CoreType.TimeOnly => ConvertToTimeOnly(obj),
#endif
                    CoreType.Guid => ConvertToGuid(obj),
                    CoreType.String => System.Convert.ToString(obj),
                    CoreType.BooleanNullable => System.Convert.ToBoolean(obj),
                    CoreType.ByteNullable => System.Convert.ToByte(obj),
                    CoreType.SByteNullable => System.Convert.ToSByte(obj),
                    CoreType.UInt16Nullable => System.Convert.ToUInt16(obj),
                    CoreType.Int16Nullable => System.Convert.ToInt16(obj),
                    CoreType.UInt32Nullable => System.Convert.ToUInt32(obj),
                    CoreType.Int32Nullable => System.Convert.ToInt32(obj),
                    CoreType.UInt64Nullable => System.Convert.ToUInt64(obj),
                    CoreType.Int64Nullable => System.Convert.ToInt64(obj),
                    CoreType.SingleNullable => System.Convert.ToSingle(obj),
                    CoreType.DoubleNullable => System.Convert.ToDouble(obj),
                    CoreType.DecimalNullable => System.Convert.ToDecimal(obj),
                    CoreType.CharNullable => System.Convert.ToChar(obj),
                    CoreType.DateTimeNullable => System.Convert.ToDateTime(obj),
                    CoreType.DateTimeOffsetNullable => System.Convert.ToDateTime(obj),
                    CoreType.TimeSpanNullable => ConvertToTimeSpan(obj),
#if NET6_0_OR_GREATER
                    CoreType.DateOnlyNullable => ConvertToDateOnly(obj),
                    CoreType.TimeOnlyNullable => ConvertToTimeOnly(obj),
#endif
                    CoreType.GuidNullable => ConvertToGuid(obj),
                    _ => throw new NotImplementedException($"Type conversion not available for {coreType}"),
                };
            }
        }

        private static Guid ConvertToGuid(object? obj)
        {
            if (obj is null)
                return Guid.Empty;
            return Guid.Parse(obj.ToString() ?? String.Empty);
        }
        private static TimeSpan ConvertToTimeSpan(object? obj)
        {
            if (obj is null)
                return TimeSpan.MinValue;
            return TimeSpan.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.InvariantCulture);
        }
#if NET6_0_OR_GREATER
        private static DateOnly ConvertToDateOnly(object? obj)
        {
            if (obj is null)
                return DateOnly.MinValue;
            return DateOnly.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.InvariantCulture);
        }
        private static TimeOnly ConvertToTimeOnly(object? obj)
        {
            if (obj is null)
                return TimeOnly.MinValue;
            return TimeOnly.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.InvariantCulture);
        }
#endif
        private static DateTimeOffset ConvertToDateTimeOffset(object? obj)
        {
            if (obj is null)
                return DateTimeOffset.MinValue;
            return DateTimeOffset.Parse(obj.ToString() ?? String.Empty, System.Globalization.CultureInfo.CurrentCulture);
        }
    }
}
