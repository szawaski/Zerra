// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Collections;
using Zerra.SourceGeneration.Reflection;
using Zerra.SourceGeneration.Types;

namespace Zerra.SourceGeneration
{
    public static class TypeAnalyzer
    {
        private static readonly ConcurrentFactoryDictionary<Type, TypeDetail> byType = new();

        public static TypeDetail GetTypeDetail(Type type)
        {
            return byType.GetOrAdd(type, GenerateTypeDetail);
        }

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

        public static T? Convert<T>(object? obj) { return (T?)Convert(obj, typeof(T)); }
        public static object? Convert(object? obj, Type type)
        {
            if (!TypeLookup.CoreTypeLookup(type, out var coreType))
                throw new NotImplementedException($"Type convert not available for {type.Name}");

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
                    _ => throw new NotImplementedException($"Type conversion not available for {type.Name}"),
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
                    _ => throw new NotImplementedException($"Type conversion not available for {type.Name}"),
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
