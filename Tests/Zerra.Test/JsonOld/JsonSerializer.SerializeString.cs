// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializerOld
    {
        public static string Serialize<T>(T? obj, JsonSerializerOptionsOld? options = null, Graph? graph = null)
        {
            if (obj == null)
                return "null";

            return ToStringJson(typeof(T), obj, options, graph);
        }
        public static string Serialize(object? obj, JsonSerializerOptionsOld? options = null, Graph? graph = null)
        {
            if (obj == null)
                return "null";

            return ToStringJson(obj.GetType(), obj, options, graph);
        }
        public static string Serialize(object? obj, Type type, JsonSerializerOptionsOld? options = null, Graph? graph = null)
        {
            if (obj == null)
                return "null";

            return ToStringJson(type, obj, options, graph);
        }

        public static byte[] SerializeBytes<T>(T? obj, JsonSerializerOptionsOld? options = null, Graph? graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToStringJson(typeof(T), obj, options, graph);
            return Encoding.UTF8.GetBytes(json);
        }
        public static byte[] SerializeBytes(object? obj, JsonSerializerOptionsOld? options = null, Graph? graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToStringJson(obj.GetType(), obj, options, graph);
            return Encoding.UTF8.GetBytes(json);
        }
        public static byte[] SerializeBytes(object? obj, Type type, JsonSerializerOptionsOld? options = null, Graph? graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToStringJson(type, obj, options, graph);
            return Encoding.UTF8.GetBytes(json);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToStringJson(Type type, object? obj, JsonSerializerOptionsOld? options, Graph? graph)
        {
            options ??= defaultOptions;
            var optionsStruct = new OptionsStruct(options);

            var writer = new CharWriterOld();
            try
            {
                var typeDetails = TypeAnalyzer.GetTypeDetail(type);
                ToStringJson(obj, typeDetails, graph, ref writer, ref optionsStruct);
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJson(Stream stream, Type type, object obj, JsonSerializerOptionsOld options, Graph? graph)
        {
            options ??= defaultOptions;
            var optionsStruct = new OptionsStruct(options);

            using var sr = new StreamReader(stream, new UTF8Encoding(false));
            var writer = new CharWriterOld(sr.ReadToEnd().ToCharArray());

            var typeDetails = TypeAnalyzer.GetTypeDetail(type);
            ToStringJson(obj, typeDetails, graph, ref writer, ref optionsStruct);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJson(object? value, TypeDetail typeDetail, Graph? graph, ref CharWriterOld writer, ref OptionsStruct options)
        {
            if (((typeDetail.Type.IsInterface && !typeDetail.HasIEnumerable) || typeDetail.Type.FullName == "System.Object") && value != null)
            {
                var objectType = value.GetType();
                typeDetail = TypeAnalyzer.GetTypeDetail(objectType);
            }

            if (value == null)
            {
                writer.Write("null");
                return;
            }

            if (typeDetail.CoreType.HasValue)
            {
                ToStringJsonCoreType(value, typeDetail.CoreType.Value, ref writer);
                return;
            }

            if (typeDetail.Type.IsEnum)
            {
                if (options.EnumAsNumber)
                {
                    ToStringJsonCoreType(value, typeDetail.EnumUnderlyingType!.Value, ref writer);
                }
                else
                {
                    writer.Write('\"');
                    writer.Write(EnumName.GetName(typeDetail.Type, value));
                    writer.Write('\"');
                }
                return;
            }
            if (typeDetail.IsNullable && typeDetail.InnerType.IsEnum)
            {
                if (options.EnumAsNumber)
                {
                    ToStringJsonCoreType(value, typeDetail.InnerTypeDetail.EnumUnderlyingType!.Value, ref writer);
                }
                else
                {
                    writer.Write('\"');
                    writer.Write(EnumName.GetName(typeDetail.InnerType, value));
                    writer.Write('\"');
                }
                return;
            }

            if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetail.SpecialType.HasValue)
            {
                ToStringJsonSpecialType(value, typeDetail, ref writer, ref options);
                return;
            }

            if (typeDetail.HasIEnumerableGeneric)
            {
                var innerTypeDetails = typeDetail.IEnumerableGenericInnerTypeDetail;
                if (typeDetail.Type.IsArray && innerTypeDetails.CoreType == CoreType.Byte)
                {
                    //special case
                    writer.Write('\"');
                    writer.Write(Convert.ToBase64String((byte[])value));
                    writer.Write('\"');
                    return;
                }
                else
                {
                    var enumerable = (IEnumerable)value;
                    writer.Write('[');
                    ToStringJsonEnumerable(enumerable, innerTypeDetails, graph, ref writer, ref options);
                    writer.Write(']');
                    return;
                }
            }

            if (!options.Nameless)
                writer.Write('{');
            else
                writer.Write('[');

            var firstProperty = true;
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                if (!member.HasGetterBoxed)
                    continue;

                if (graph != null && !graph.HasMember(member.Name))
                    continue;

                var propertyValue = member.GetterBoxed(value);

                if (options.DoNotWriteNullProperties && propertyValue == null)
                    continue;

                if (firstProperty)
                    firstProperty = false;
                else
                    writer.Write(',');

                if (!options.Nameless)
                {
                    writer.Write('\"');
                    writer.Write(GetMemberName(member));
                    writer.Write('\"');
                    writer.Write(':');
                }

                var childGraph = graph?.GetChildGraph(member.Name);
                ToStringJson(propertyValue, member.TypeDetail, childGraph, ref writer, ref options);
            }

            if (!options.Nameless)
                writer.Write('}');
            else
                writer.Write(']');
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonEnumerable(IEnumerable values, TypeDetail typeDetail, Graph? graph, ref CharWriterOld writer, ref OptionsStruct options)
        {
            if (typeDetail.CoreType.HasValue)
            {
                ToStringJsonCoreTypeEnumerable(values, typeDetail.CoreType.Value, ref writer);
                return;
            }

            if (typeDetail.Type.IsEnum)
            {
                var first = true;
                foreach (var value in values)
                {
                    if (first)
                        first = false;
                    else
                        writer.Write(',');
                    if (value != null)
                    {
                        if (options.EnumAsNumber)
                        {
                            ToStringJsonCoreType(value, typeDetail.EnumUnderlyingType!.Value, ref writer);
                        }
                        else
                        {
                            writer.Write('\"');
                            writer.Write(EnumName.GetName(typeDetail.Type, value));
                            writer.Write('\"');
                        }
                    }
                    else
                    {
                        writer.Write("null");
                    }
                }
                return;
            }
            if (typeDetail.IsNullable && typeDetail.InnerType.IsEnum)
            {
                var first = true;
                foreach (var value in values)
                {
                    if (first)
                        first = false;
                    else
                        writer.Write(',');
                    if (value != null)
                    {
                        if (options.EnumAsNumber)
                        {
                            ToStringJsonCoreType(value, typeDetail.InnerTypeDetail.EnumUnderlyingType!.Value, ref writer);
                        }
                        else
                        {
                            writer.Write('\"');
                            writer.Write(EnumName.GetName(typeDetail.InnerType, (Enum)value));
                            writer.Write('\"');
                        }
                    }
                    else
                    {
                        writer.Write("null");
                    }
                }
                return;
            }

            if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetail.SpecialType.HasValue)
            {
                ToStringJsonSpecialTypeEnumerable(values, typeDetail, ref writer, ref options);
                return;
            }

            if (typeDetail.HasIEnumerableGeneric)
            {
                var first = true;
                foreach (var value in values)
                {
                    if (first)
                        first = false;
                    else
                        writer.Write(',');
                    if (value != null)
                    {
                        var enumerable = (IEnumerable)value;
                        writer.Write('[');
                        ToStringJsonEnumerable(enumerable, typeDetail.IEnumerableGenericInnerTypeDetail, graph, ref writer, ref options);
                        writer.Write(']');
                    }
                    else
                    {
                        writer.Write("null");
                    }
                }
                return;
            }

            {
                var first = true;
                foreach (var value in values)
                {
                    if (first)
                        first = false;
                    else
                        writer.Write(',');
                    if (value != null)
                    {
                        if (!options.Nameless)
                            writer.Write('{');
                        else
                            writer.Write('[');

                        var firstProperty = true;
                        foreach (var member in typeDetail.SerializableMemberDetails)
                        {
                            if (!member.HasGetterBoxed)
                                continue;

                            if (graph != null && !graph.HasMember(member.Name))
                                continue;

                            var propertyValue = member.GetterBoxed(value);

                            if (options.DoNotWriteNullProperties && propertyValue == null)
                                continue;

                            if (firstProperty)
                                firstProperty = false;
                            else
                                writer.Write(',');

                            if (!options.Nameless)
                            {
                                writer.Write('\"');
                                writer.Write(GetMemberName(member));
                                writer.Write('\"');
                                writer.Write(':');
                            }


                            var childGraph = graph?.GetChildGraph(member.Name);
                            ToStringJson(propertyValue, member.TypeDetail, childGraph, ref writer, ref options);
                        }

                        if (!options.Nameless)
                            writer.Write('}');
                        else
                            writer.Write(']');
                    }
                    else
                    {
                        writer.Write("null");
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonCoreType(object value, CoreType coreType, ref CharWriterOld writer)
        {
            switch (coreType)
            {
                case CoreType.String:
                    ToStringJsonString((string)value, ref writer);
                    return;

                case CoreType.Boolean:
                case CoreType.BooleanNullable:
                    writer.Write((bool)value == false ? "false" : "true");
                    return;
                case CoreType.Byte:
                case CoreType.ByteNullable:
                    writer.Write((byte)value);
                    return;
                case CoreType.SByte:
                case CoreType.SByteNullable:
                    writer.Write((sbyte)value);
                    return;
                case CoreType.Int16:
                case CoreType.Int16Nullable:
                    writer.Write((short)value);
                    return;
                case CoreType.UInt16:
                case CoreType.UInt16Nullable:
                    writer.Write((ushort)value);
                    return;
                case CoreType.Int32:
                case CoreType.Int32Nullable:
                    writer.Write((int)value);
                    return;
                case CoreType.UInt32:
                case CoreType.UInt32Nullable:
                    writer.Write((uint)value);
                    return;
                case CoreType.Int64:
                case CoreType.Int64Nullable:
                    writer.Write((long)value);
                    return;
                case CoreType.UInt64:
                case CoreType.UInt64Nullable:
                    writer.Write((ulong)value);
                    return;
                case CoreType.Single:
                case CoreType.SingleNullable:
                    writer.Write((float)value);
                    return;
                case CoreType.Double:
                case CoreType.DoubleNullable:
                    writer.Write((double)value);
                    return;
                case CoreType.Decimal:
                case CoreType.DecimalNullable:
                    writer.Write((decimal)value);
                    return;
                case CoreType.Char:
                case CoreType.CharNullable:
                    ToStringJsonChar((char)value, ref writer);
                    return;
                case CoreType.DateTime:
                case CoreType.DateTimeNullable:
                    writer.Write('\"');
                    writer.Write((DateTime)value, DateTimeFormatOld.ISO8601);
                    writer.Write('\"');
                    return;
                case CoreType.DateTimeOffset:
                case CoreType.DateTimeOffsetNullable:
                    writer.Write('\"');
                    writer.Write((DateTimeOffset)value, DateTimeFormatOld.ISO8601);
                    writer.Write('\"');
                    return;
                case CoreType.TimeSpan:
                case CoreType.TimeSpanNullable:
                    writer.Write('\"');
                    writer.Write((TimeSpan)value, TimeFormatOld.ISO8601);
                    writer.Write('\"');
                    return;
#if NET6_0_OR_GREATER
                case CoreType.DateOnly:
                case CoreType.DateOnlyNullable:
                    writer.Write('\"');
                    writer.Write((DateOnly)value, DateTimeFormatOld.ISO8601);
                    writer.Write('\"');
                    return;
                case CoreType.TimeOnly:
                case CoreType.TimeOnlyNullable:
                    writer.Write('\"');
                    writer.Write((TimeOnly)value, TimeFormatOld.ISO8601);
                    writer.Write('\"');
                    return;
#endif
                case CoreType.Guid:
                case CoreType.GuidNullable:
                    writer.Write('\"');
                    writer.Write((Guid)value);
                    writer.Write('\"');
                    return;

                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonCoreType(object value, CoreEnumType coreType, ref CharWriterOld writer)
        {
            switch (coreType)
            {
                case CoreEnumType.Byte:
                case CoreEnumType.ByteNullable:
                    writer.Write((byte)value);
                    return;
                case CoreEnumType.SByte:
                case CoreEnumType.SByteNullable:
                    writer.Write((sbyte)value);
                    return;
                case CoreEnumType.Int16:
                case CoreEnumType.Int16Nullable:
                    writer.Write((short)value);
                    return;
                case CoreEnumType.UInt16:
                case CoreEnumType.UInt16Nullable:
                    writer.Write((ushort)value);
                    return;
                case CoreEnumType.Int32:
                case CoreEnumType.Int32Nullable:
                    writer.Write((int)value);
                    return;
                case CoreEnumType.UInt32:
                case CoreEnumType.UInt32Nullable:
                    writer.Write((uint)value);
                    return;
                case CoreEnumType.Int64:
                case CoreEnumType.Int64Nullable:
                    writer.Write((long)value);
                    return;
                case CoreEnumType.UInt64:
                case CoreEnumType.UInt64Nullable:
                    writer.Write((ulong)value);
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonCoreTypeEnumerable(IEnumerable values, CoreType coreType, ref CharWriterOld writer)
        {
            switch (coreType)
            {
                case CoreType.String:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<string>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');

                            ToStringJsonString(value, ref writer);
                        }
                    }
                    return;

                case CoreType.Boolean:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<bool>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value == false ? "false" : "true");
                        }
                    }
                    return;
                case CoreType.Byte:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<byte>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.SByte:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<sbyte>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Int16:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<short>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.UInt16:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<ushort>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Int32:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<int>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.UInt32:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<uint>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Int64:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<long>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.UInt64:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<ulong>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Single:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<float>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Double:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<double>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Decimal:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<decimal>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Char:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<char>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            ToStringJsonChar(value, ref writer);
                        }
                    }
                    return;
                case CoreType.DateTime:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<DateTime>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, DateTimeFormatOld.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
                case CoreType.DateTimeOffset:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<DateTimeOffset>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, DateTimeFormatOld.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
                case CoreType.TimeSpan:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<TimeSpan>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, TimeFormatOld.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
#if NET6_0_OR_GREATER
                case CoreType.DateOnly:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<DateOnly>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, DateTimeFormatOld.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
                case CoreType.TimeOnly:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<TimeOnly>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, TimeFormatOld.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
#endif
                case CoreType.Guid:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<Guid>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value);
                            writer.Write('\"');
                        }
                    }
                    return;

                case CoreType.BooleanNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<bool?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value == false ? "false" : "true");
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.ByteNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<byte?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.SByteNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<sbyte?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.Int16Nullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<short?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.UInt16Nullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<ushort?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.Int32Nullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<int?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.UInt32Nullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<uint?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.Int64Nullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<long?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.UInt64Nullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<ulong?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.SingleNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<float?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.DoubleNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<double?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.DecimalNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<decimal?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.CharNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<char?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                ToStringJsonChar(value.Value, ref writer);
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
                case CoreType.DateTimeNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<DateTime?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, DateTimeFormatOld.ISO8601);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
                case CoreType.DateTimeOffsetNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<DateTimeOffset?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, DateTimeFormatOld.ISO8601);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
                case CoreType.TimeSpanNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<TimeSpan?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, TimeFormatOld.ISO8601);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
#if NET6_0_OR_GREATER
                case CoreType.DateOnlyNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<DateOnly?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, DateTimeFormatOld.ISO8601);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
                case CoreType.TimeOnlyNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<TimeOnly?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, TimeFormatOld.ISO8601);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
#endif
                case CoreType.GuidNullable:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<Guid?>)values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;

                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonSpecialType(object value, TypeDetail typeDetail, ref CharWriterOld writer, ref OptionsStruct options)
        {
            var specialType = typeDetail.IsNullable ? typeDetail.InnerTypeDetail.SpecialType!.Value : typeDetail.SpecialType!.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var valueType = value == null ? null : (Type)value;
                        if (valueType != null)
                            ToStringJsonString(valueType.FullName, ref writer);
                        else
                            writer.Write("null");
                    }
                    return;
                case SpecialType.Dictionary:
                    {
                        if (value != null)
                        {
                            var innerTypeDetail = typeDetail.DictionaryInnerTypeDetail;

                            var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").GetterBoxed;
                            var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").GetterBoxed;
                            var method = TypeAnalyzer.GetGenericMethodDetail(dictionaryToArrayMethod, typeDetail.DictionaryInnerType);

                            var innerValue = (ICollection)method.Caller(null, new object[] { value })!;
                            if (!options.Nameless)
                                writer.Write('{');
                            else
                                writer.Write('[');
                            var firstkvp = true;
                            foreach (var kvp in innerValue)
                            {
                                if (firstkvp)
                                    firstkvp = false;
                                else
                                    writer.Write(',');
                                var kvpKey = keyGetter(kvp)!;
                                var kvpValue = valueGetter(kvp);
                                if (!options.Nameless)
                                {
                                    ToStringJsonString(kvpKey.ToString(), ref writer);
                                    writer.Write(':');
                                    ToStringJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, ref options);
                                }
                                else
                                {
                                    writer.Write('[');
                                    ToStringJson(kvpKey, innerTypeDetail.InnerTypeDetails[0], null, ref writer, ref options);
                                    writer.Write(',');
                                    ToStringJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, ref options);
                                    writer.Write(']');
                                }
                            }
                            if (!options.Nameless)
                                writer.Write('}');
                            else
                                writer.Write(']');
                        }
                        else
                        {
                            writer.Write("null");
                        }
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonSpecialTypeEnumerable(IEnumerable values, TypeDetail typeDetail, ref CharWriterOld writer, ref OptionsStruct options)
        {
            var specialType = typeDetail.IsNullable ? typeDetail.InnerTypeDetail.SpecialType!.Value : typeDetail.SpecialType!.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var first = true;
                        foreach (var value in values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            var valueType = value == null ? null : (Type)value;
                            if (valueType != null)
                                ToStringJsonString(valueType.FullName, ref writer);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case SpecialType.Dictionary:
                    {
                        var first = true;
                        foreach (var value in values)
                        {
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value != null)
                            {
                                var innerTypeDetail = typeDetail.InnerTypeDetail;

                                var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").GetterBoxed;
                                var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").GetterBoxed;
                                var method = TypeAnalyzer.GetGenericMethodDetail(dictionaryToArrayMethod, typeDetail.InnerTypes[0]);

                                var innerValue = (ICollection)method.Caller(null, new object[] { value })!;
                                if (!options.Nameless)
                                    writer.Write('{');
                                else
                                    writer.Write('[');
                                var firstkvp = true;
                                foreach (var kvp in innerValue)
                                {
                                    if (firstkvp)
                                        firstkvp = false;
                                    else
                                        writer.Write(',');
                                    var kvpKey = keyGetter(kvp)!;
                                    var kvpValue = valueGetter(kvp);
                                    if (!options.Nameless)
                                    {
                                        ToStringJsonString(kvpKey.ToString(), ref writer);
                                        writer.Write(':');
                                        ToStringJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, ref options);
                                    }
                                    else
                                    {
                                        writer.Write('[');
                                        ToStringJson(kvpKey, innerTypeDetail.InnerTypeDetails[0], null, ref writer, ref options);
                                        writer.Write(',');
                                        ToStringJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, ref options);
                                        writer.Write(']');
                                    }
                                }
                                if (!options.Nameless)
                                    writer.Write('}');
                                else
                                    writer.Write(']');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
                default:
                    throw new NotImplementedException();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ToStringJsonString(string? value, ref CharWriterOld writer)
        {
            if (value == null)
            {
                writer.Write("null");
                return;
            }
            writer.Write('\"');
            if (value.Length == 0)
            {
                writer.Write('\"');
                return;
            }

            var chars = value.AsSpan();

            var start = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                char escapedChar;
                switch (c)
                {
                    case '"':
                        escapedChar = '"';
                        break;
                    case '\\':
                        escapedChar = '\\';
                        break;
                    case '\b':
                        escapedChar = 'b';
                        break;
                    case '\f':
                        escapedChar = 'f';
                        break;
                    case '\n':
                        escapedChar = 'n';
                        break;
                    case '\r':
                        escapedChar = 'r';
                        break;
                    case '\t':
                        escapedChar = 't';
                        break;
                    default:
                        if (c >= ' ')
                            continue;

                        writer.Write(chars.Slice(start, i - start));
                        start = i + 1;
                        var code = lowUnicodeIntToEncodedHex[c];
                        writer.Write(code);
                        continue;
                }

                writer.Write(chars.Slice(start, i - start));
                start = i + 1;
                writer.Write('\\');
                writer.Write(escapedChar);
            }

            if (start != chars.Length)
                writer.Write(chars.Slice(start, chars.Length - start));
            writer.Write('\"');
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToStringJsonChar(char value, ref CharWriterOld writer)
        {
            switch (value)
            {
                case '\\':
                    writer.Write("\"\\\\\"");
                    return;
                case '"':
                    writer.Write("\"\\\"\"");
                    return;
                case '/':
                    writer.Write("\"\\/\"");
                    return;
                case '\b':
                    writer.Write("\"\\b\"");
                    return;
                case '\t':
                    writer.Write("\"\\t\"");
                    return;
                case '\n':
                    writer.Write("\"\\n\"");
                    return;
                case '\f':
                    writer.Write("\"\\f\"");
                    return;
                case '\r':
                    writer.Write("\"\\r\"");
                    return;
                default:
                    if (value < ' ')
                    {
                        var code = lowUnicodeIntToEncodedHex[value];
                        writer.Write("\"");
                        writer.Write(code);
                        writer.Write("\"");
                        return;
                    }
                    writer.Write("\"");
                    writer.Write(value);
                    writer.Write("\"");
                    return;
            }
        }
    }
}