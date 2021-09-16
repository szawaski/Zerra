// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        private static readonly MethodInfo dictionaryToArrayMethod = typeof(System.Linq.Enumerable).GetMethod("ToArray");

        public static string Serialize<T>(T obj, Graph graph = null)
        {
            if (obj == null)
                return "null";

            return ToJson(typeof(T), obj, graph);
        }
        public static string Serialize(object obj, Graph graph = null)
        {
            if (obj == null)
                return "null";

            return ToJson(obj.GetType(), obj, graph);
        }
        public static string Serialize(object obj, Type type, Graph graph = null)
        {
            if (obj == null)
                return "null";

            return ToJson(type, obj, graph);
        }

        public static byte[] SerializeBytes<T>(T obj, Graph graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToJson(typeof(T), obj, graph);
            return Encoding.UTF8.GetBytes(json);
        }
        public static byte[] SerializeBytes(object obj, Graph graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToJson(obj.GetType(), obj, graph);
            return Encoding.UTF8.GetBytes(json);
        }
        public static byte[] SerializeBytes(object obj, Type type, Graph graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToJson(type, obj, graph);
            return Encoding.UTF8.GetBytes(json);
        }

        public static void Serialize<T>(Stream stream, T obj, Graph graph = null)
        {
            if (obj == null)
                return;

            ToJson(stream, typeof(T), obj, graph);
        }
        public static void Serialize(Stream stream, object obj, Graph graph = null)
        {
            if (obj == null)
                return;

            ToJson(stream, obj.GetType(), obj, graph);
        }
        public static void Serialize(Stream stream, object obj, Type type, Graph graph = null)
        {
            if (obj == null)
                return;

            ToJson(stream, type, obj, graph);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToJson(Type type, object obj, Graph graph = null)
        {
            var writer = new CharWriteBuffer();
            try
            {
                var typeDetails = TypeAnalyzer.GetType(type);
                ToJson(obj, typeDetails, graph, ref writer, false);
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToJson(Stream stream, Type type, object obj, Graph graph = null)
        {
            var writer = new CharWriteBuffer(stream, new UTF8Encoding(false));
            try
            {
                var typeDetails = TypeAnalyzer.GetType(type);
                ToJson(obj, typeDetails, graph, ref writer, false);
                writer.Flush();
            }
            finally
            {
                writer.Dispose();
            }
        }

        public static string SerializeNameless<T>(T obj, Graph graph = null)
        {
            if (obj == null)
                return "null";

            return ToJsonNameless(typeof(T), obj, graph);
        }
        public static string SerializeNameless(object obj, Graph graph = null)
        {
            if (obj == null)
                return "null";

            return ToJsonNameless(obj.GetType(), obj, graph);
        }
        public static string SerializeNameless(object obj, Type type, Graph graph = null)
        {
            if (obj == null)
                return "null";

            return ToJsonNameless(type, obj, graph);
        }

        public static byte[] SerializeNamelessBytes<T>(T obj, Graph graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToJsonNameless(typeof(T), obj, graph);
            return Encoding.UTF8.GetBytes(json);
        }
        public static byte[] SerializeNamelessBytes(object obj, Graph graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToJsonNameless(obj.GetType(), obj, graph);
            return Encoding.UTF8.GetBytes(json);
        }
        public static byte[] SerializeNamelessBytes(object obj, Type type, Graph graph = null)
        {
            if (obj == null)
                return Encoding.UTF8.GetBytes("null");

            var json = ToJsonNameless(type, obj, graph);
            return Encoding.UTF8.GetBytes(json);
        }

        public static void SerializeNameless<T>(Stream stream, T obj, Graph graph = null)
        {
            if (obj == null)
                return;

            ToJsonNameless(stream, typeof(T), obj, graph);
        }
        public static void SerializeNameless(Stream stream, object obj, Graph graph = null)
        {
            if (obj == null)
                return;

            ToJsonNameless(stream, obj.GetType(), obj, graph);
        }
        public static void SerializeNameless(Stream stream, object obj, Type type, Graph graph = null)
        {
            if (obj == null)
                return;

            ToJsonNameless(stream, type, obj, graph);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ToJsonNameless(Type type, object obj, Graph graph = null)
        {
            var writer = new CharWriteBuffer();
            try
            {
                var typeDetails = TypeAnalyzer.GetType(type);
                ToJson(obj, typeDetails, graph, ref writer, true);
                return writer.ToString();
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToJsonNameless(Stream stream,Type type, object obj, Graph graph = null)
        {
            var writer = new CharWriteBuffer(stream, new UTF8Encoding(false));
            try
            {
                var typeDetails = TypeAnalyzer.GetType(type);
                ToJson(obj, typeDetails, graph, ref writer, true);
                writer.Flush();
            }
            finally
            {
                writer.Dispose();
            }
        }

        private static void ToJson(object value, TypeDetail typeDetail, Graph graph, ref CharWriteBuffer writer, bool nameless)
        {
            if (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable)
            {
                var objectType = value.GetType();
                typeDetail = TypeAnalyzer.GetType(objectType);
            }

            if (value == null)
            {
                writer.Write("null");
                return;
            }

            if (typeDetail.CoreType.HasValue)
            {
                ToJsonCoreType(value, typeDetail.CoreType.Value, ref writer);
                return;
            }

            if (typeDetail.Type.IsEnum)
            {
                writer.Write('\"');
                writer.Write(EnumName.GetEnumName(((Enum)value)));
                writer.Write('\"');
                return;
            }
            if (typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                writer.Write('\"');
                writer.Write(EnumName.GetEnumName(((Enum)value)));
                writer.Write('\"');
                return;
            }

            if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].SpecialType.HasValue)
            {
                ToJsonSpecialType(value, typeDetail, ref writer, nameless);
                return;
            }

            if (typeDetail.IsIEnumerableGeneric)
            {
                var innerTypeDetails = typeDetail.IEnumerableGenericInnerTypeDetails;
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
                    var enumerable = value as IEnumerable;
                    writer.Write('[');
                    ToJsonEnumerable(enumerable, innerTypeDetails, graph, ref writer, nameless);
                    writer.Write(']');
                    return;
                }
            }

            if (!nameless)
                writer.Write('{');
            else
                writer.Write('[');

            bool firstProperty = true;
            foreach (var member in typeDetail.SerializableMemberDetails)
            {
                if (graph != null)
                {
                    if (member.TypeDetail.IsGraphLocalProperty)
                    {
                        if (!graph.HasLocalProperty(member.Name))
                            continue;
                    }
                    else
                    {
                        if (!graph.HasChildGraph(member.Name))
                            continue;
                    }
                }

                if (firstProperty) firstProperty = false;
                else writer.Write(',');

                if (!nameless)
                {
                    writer.Write('\"');
                    writer.Write(member.Name);
                    writer.Write('\"');
                    writer.Write(':');
                }

                object propertyValue = member.Getter(value);
                var childGraph = graph?.GetChildGraph(member.Name);
                ToJson(propertyValue, member.TypeDetail, childGraph, ref writer, nameless);
            }

            if (!nameless)
                writer.Write('}');
            else
                writer.Write(']');
        }
        private static void ToJsonEnumerable(IEnumerable values, TypeDetail typeDetail, Graph graph, ref CharWriteBuffer writer, bool nameless)
        {
            if (typeDetail.CoreType.HasValue)
            {
                ToJsonCoreTypeEnumerabale(values, typeDetail.CoreType.Value, ref writer);
                return;
            }

            if (typeDetail.Type.IsEnum)
            {
                bool first = true;
                foreach (var value in values)
                {
                    if (first) first = false;
                    else writer.Write(',');
                    writer.Write('\"');
                    writer.Write(EnumName.GetEnumName(((Enum)value)));
                    writer.Write('\"');
                }
                return;
            }
            if (typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                bool first = true;
                foreach (var value in values)
                {
                    if (first) first = false;
                    else writer.Write(',');
                    if (value != null)
                    {
                        writer.Write('\"');
                        writer.Write(EnumName.GetEnumName(((Enum)value)));
                        writer.Write('\"');
                    }
                    else
                    {
                        writer.Write("null");
                    }
                }
                return;
            }

            if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].SpecialType.HasValue)
            {
                writer.Write('[');
                ToJsonSpecialTypeEnumerable(values, typeDetail, ref writer, nameless);
                writer.Write(']');
                return;
            }

            if (typeDetail.IsIEnumerableGeneric)
            {
                bool first = true;
                foreach (var value in values)
                {
                    if (first) first = false;
                    else writer.Write(',');
                    if (value != null)
                    {
                        var enumerable = value as IEnumerable;
                        writer.Write('[');
                        ToJsonEnumerable(enumerable, typeDetail.IEnumerableGenericInnerTypeDetails, graph, ref writer, nameless);
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
                bool first = true;
                foreach (var value in values)
                {
                    if (first) first = false;
                    else writer.Write(',');
                    if (!nameless)
                        writer.Write('{');
                    else
                        writer.Write('[');

                    bool firstProperty = true;
                    foreach (var member in typeDetail.SerializableMemberDetails)
                    {
                        if (graph != null)
                        {
                            if (member.TypeDetail.IsGraphLocalProperty)
                            {
                                if (!graph.HasLocalProperty(member.Name))
                                    continue;
                            }
                            else
                            {
                                if (!graph.HasChildGraph(member.Name))
                                    continue;
                            }
                        }

                        if (firstProperty) firstProperty = false;
                        else writer.Write(',');

                        if (!nameless)
                        {
                            writer.Write('\"');
                            writer.Write(member.Name);
                            writer.Write('\"');
                            writer.Write(':');
                        }

                        object propertyValue = member.Getter(value);
                        var childGraph = graph?.GetChildGraph(member.Name);
                        ToJson(propertyValue, member.TypeDetail, childGraph, ref writer, nameless);
                    }

                    if (!nameless)
                        writer.Write('}');
                    else
                        writer.Write(']');
                }
            }
        }
        private static void ToJsonCoreType(object value, CoreType coreType, ref CharWriteBuffer writer)
        {
            switch (coreType)
            {
                case CoreType.String:
                    ToJsonString((string)value, ref writer);
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
                    ToJsonChar((char)value, ref writer);
                    return;
                case CoreType.DateTime:
                case CoreType.DateTimeNullable:
                    writer.Write('\"');
                    writer.Write((DateTime)value, DateTimeFormat.ISO8601);
                    writer.Write('\"');
                    return;
                case CoreType.DateTimeOffset:
                case CoreType.DateTimeOffsetNullable:
                    writer.Write('\"');
                    writer.Write((DateTimeOffset)value, DateTimeFormat.ISO8601);
                    writer.Write('\"');
                    return;
                case CoreType.TimeSpan:
                case CoreType.TimeSpanNullable:
                    writer.Write('\"');
                    writer.Write((TimeSpan)value, TimeFormat.ISO8601);
                    writer.Write('\"');
                    return;
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
        private static void ToJsonCoreTypeEnumerabale(IEnumerable values, CoreType coreType, ref CharWriteBuffer writer)
        {
            switch (coreType)
            {
                case CoreType.String:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<string>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');

                            ToJsonString(value, ref writer);
                        }
                    }
                    return;

                case CoreType.Boolean:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<bool>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value == false ? "false" : "true");
                        }
                    }
                    return;
                case CoreType.Byte:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<byte>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.SByte:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<sbyte>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Int16:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<short>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.UInt16:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<ushort>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Int32:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<int>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.UInt32:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<uint>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Int64:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<long>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.UInt64:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<ulong>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Single:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<float>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Double:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<double>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Decimal:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<decimal>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write(value);
                        }
                    }
                    return;
                case CoreType.Char:
                    {
                        var first = true;
                        foreach (var value in (IEnumerable<char>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            ToJsonChar(value, ref writer);
                        }
                    }
                    return;
                case CoreType.DateTime:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<DateTime>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, DateTimeFormat.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
                case CoreType.DateTimeOffset:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<DateTimeOffset>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, DateTimeFormat.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
                case CoreType.TimeSpan:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<TimeSpan>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value, TimeFormat.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
                case CoreType.Guid:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<Guid>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            writer.Write('\"');
                            writer.Write(value);
                            writer.Write('\"');
                        }
                    }
                    return;

                case CoreType.BooleanNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<bool?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value == false ? "false" : "true");
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.ByteNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<byte?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.SByteNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<sbyte?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.Int16Nullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<short?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.UInt16Nullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<ushort?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.Int32Nullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<int?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.UInt32Nullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<uint?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.Int64Nullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<long?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.UInt64Nullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<ulong?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.SingleNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<float?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.DoubleNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<double?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.DecimalNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<decimal?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                                writer.Write(value.Value);
                            else
                                writer.Write("null");
                        }
                    }
                    return;
                case CoreType.CharNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<char?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                            {
                                ToJsonChar(value.Value, ref writer);
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
                        bool first = true;
                        foreach (var value in (IEnumerable<DateTime?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, DateTimeFormat.ISO8601);
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
                        bool first = true;
                        foreach (var value in (IEnumerable<DateTimeOffset?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, DateTimeFormat.ISO8601);
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
                        bool first = true;
                        foreach (var value in (IEnumerable<TimeSpan?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            if (value.HasValue)
                            {
                                writer.Write('\"');
                                writer.Write(value.Value, TimeFormat.ISO8601);
                                writer.Write('\"');
                            }
                            else
                            {
                                writer.Write("null");
                            }
                        }
                    }
                    return;
                case CoreType.GuidNullable:
                    {
                        bool first = true;
                        foreach (var value in (IEnumerable<Guid?>)values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
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
        private static void ToJsonSpecialType(object value, TypeDetail typeDetail, ref CharWriteBuffer writer, bool nameless)
        {
            var specialType = typeDetail.IsNullable ? typeDetail.InnerTypeDetails[0].SpecialType.Value : typeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var valueType = value == null ? null : (Type)value;
                        if (valueType != null)
                            ToJsonString(valueType.FullName, ref writer);
                        else
                            writer.Write("null");
                    }
                    return;
                case SpecialType.Dictionary:
                    {
                        if (value != null)
                        {
                            var innerTypeDetail = typeDetail.InnerTypeDetails[0];

                            var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                            var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").Getter;
                            var method = TypeAnalyzer.GetGenericMethod(dictionaryToArrayMethod, typeDetail.InnerTypes[0]);

                            var innerValue = (ICollection)method.Caller(null, new object[] { value });
                            if (!nameless)
                                writer.Write('{');
                            else
                                writer.Write('[');
                            var firstkvp = true;
                            foreach (var kvp in innerValue)
                            {
                                if (firstkvp) firstkvp = false;
                                else writer.Write(',');
                                var kvpKey = keyGetter(kvp);
                                var kvpValue = valueGetter(kvp);
                                if (!nameless)
                                {
                                    ToJsonString(kvpKey.ToString(), ref writer);
                                    writer.Write(':');
                                    ToJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, nameless);
                                }
                                else
                                {
                                    writer.Write('[');
                                    ToJson(kvpKey, innerTypeDetail.InnerTypeDetails[0], null, ref writer, nameless);
                                    writer.Write(',');
                                    ToJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, nameless);
                                    writer.Write(']');
                                }
                            }
                            if (!nameless)
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
        private static void ToJsonSpecialTypeEnumerable(IEnumerable values, TypeDetail typeDetail, ref CharWriteBuffer writer, bool nameless)
        {
            var specialType = typeDetail.IsNullable ? typeDetail.InnerTypeDetails[0].SpecialType.Value : typeDetail.SpecialType.Value;
            switch (specialType)
            {
                case SpecialType.Type:
                    {
                        var first = true;
                        foreach (var value in values)
                        {
                            if (first) first = false;
                            else writer.Write(',');
                            var valueType = value == null ? null : (Type)value;
                            if (valueType != null)
                                ToJsonString(valueType.FullName, ref writer);
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
                            if (first) first = false;
                            else writer.Write(',');
                            if (value != null)
                            {
                                var innerTypeDetail = typeDetail.InnerTypeDetails[0];

                                var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                                var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").Getter;
                                var method = TypeAnalyzer.GetGenericMethod(dictionaryToArrayMethod, typeDetail.InnerTypes[0]);

                                var innerValue = (ICollection)method.Caller(null, new object[] { value });
                                if (!nameless)
                                    writer.Write('{');
                                else
                                    writer.Write('[');
                                var firstkvp = true;
                                foreach (var kvp in innerValue)
                                {
                                    if (firstkvp) firstkvp = false;
                                    else writer.Write(',');
                                    var kvpKey = keyGetter(kvp);
                                    var kvpValue = valueGetter(kvp);
                                    if (!nameless)
                                    {
                                        ToJsonString(kvpKey.ToString(), ref writer);
                                        writer.Write(':');
                                        ToJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, nameless);
                                    }
                                    else
                                    {
                                        writer.Write('[');
                                        ToJson(kvpKey, innerTypeDetail.InnerTypeDetails[0], null, ref writer, nameless);
                                        writer.Write(',');
                                        ToJson(kvpValue, innerTypeDetail.InnerTypeDetails[1], null, ref writer, nameless);
                                        writer.Write(']');
                                    }
                                }
                                if (!nameless)
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
        internal static void ToJsonString(string value, ref CharWriteBuffer writer)
        {
            writer.Write('\"');
            if (value == null || value.Length == 0)
            {
                writer.Write('\"');
                return;
            }

            var chars = value.AsSpan();

            var start = 0;
            char escapedChar = default(char);
            for (int i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
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
                        if (c < ' ')
                        {
                            var code = lowUnicodeIntToEncodedHex[c];
                            writer.Write(code);
                            break;
                        }
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
        private static void ToJsonChar(char value, ref CharWriteBuffer writer)
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