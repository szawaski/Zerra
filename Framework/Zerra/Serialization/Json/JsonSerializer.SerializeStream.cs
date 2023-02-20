// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        public static Task SerializeAsync(Stream stream, object obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return Task.CompletedTask;

            var type = obj.GetType();

            return SerializeAsync(stream, obj, type, graph);
        }
        public static Task SerializeAsync<T>(Stream stream, T obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return Task.CompletedTask;

            var type = typeof(T);

            return SerializeAsync(stream, obj, type, graph);
        }
        public static async Task SerializeAsync(Stream stream, object obj, Type type, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState();
                state.CurrentFrame = new WriteFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value };

                for (; ; )
                {
                    Write(buffer, ref state);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, state.BufferPostion);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, state.BufferPostion));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Write(Span<byte> buffer, ref WriteState state)
        {
            var writer = new ByteWriter(buffer, encoding);
            for (; ; )
            {
                switch (state.CurrentFrame.FrameType)
                {
                    case ReadFrameType.Value: break;
                }
                if (state.Ended)
                {
                    state.BufferPostion = writer.Position;
                    return;
                }
                if (state.BytesNeeded > 0)
                {
                    state.BufferPostion = writer.Position;
                    return;
                }
            }
        }

        //public static string SerializeNameless<T>(T obj, Graph graph = null)
        //{
        //    if (obj == null)
        //        return "null";

        //    return ToJsonNameless(typeof(T), obj, graph);
        //}
        //public static string SerializeNameless(object obj, Graph graph = null)
        //{
        //    if (obj == null)
        //        return "null";

        //    return ToJsonNameless(obj.GetType(), obj, graph);
        //}
        //public static string SerializeNameless(object obj, Type type, Graph graph = null)
        //{
        //    if (obj == null)
        //        return "null";

        //    return ToJsonNameless(type, obj, graph);
        //}

        //public static byte[] SerializeNamelessBytes<T>(T obj, Graph graph = null)
        //{
        //    if (obj == null)
        //        return Encoding.UTF8.GetBytes("null");

        //    var json = ToJsonNameless(typeof(T), obj, graph);
        //    return Encoding.UTF8.GetBytes(json);
        //}
        //public static byte[] SerializeNamelessBytes(object obj, Graph graph = null)
        //{
        //    if (obj == null)
        //        return Encoding.UTF8.GetBytes("null");

        //    var json = ToJsonNameless(obj.GetType(), obj, graph);
        //    return Encoding.UTF8.GetBytes(json);
        //}
        //public static byte[] SerializeNamelessBytes(object obj, Type type, Graph graph = null)
        //{
        //    if (obj == null)
        //        return Encoding.UTF8.GetBytes("null");

        //    var json = ToJsonNameless(type, obj, graph);
        //    return Encoding.UTF8.GetBytes(json);
        //}

        //public static void SerializeNameless<T>(Stream stream, T obj, Graph graph = null)
        //{
        //    if (obj == null)
        //        return;

        //    ToJsonNameless(stream, typeof(T), obj, graph);
        //}
        //public static void SerializeNameless(Stream stream, object obj, Graph graph = null)
        //{
        //    if (obj == null)
        //        return;

        //    ToJsonNameless(stream, obj.GetType(), obj, graph);
        //}
        //public static void SerializeNameless(Stream stream, object obj, Type type, Graph graph = null)
        //{
        //    if (obj == null)
        //        return;

        //    ToJsonNameless(stream, type, obj, graph);
        //}

        private static void WriteJson(object value, TypeDetail typeDetail, Graph graph, ref CharWriter writer, bool nameless)
        {
            if (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable)
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
                ToJsonCoreType(value, typeDetail.CoreType.Value, ref writer);
                return;
            }

            if (typeDetail.Type.IsEnum)
            {
                writer.Write('\"');
                writer.Write(EnumName.GetName(typeDetail.Type, value));
                writer.Write('\"');
                return;
            }
            if (typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                writer.Write('\"');
                writer.Write(EnumName.GetName(typeDetail.InnerTypes[0], value));
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

            var firstProperty = true;
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

                if (firstProperty)
                    firstProperty = false;
                else
                    writer.Write(',');

                if (!nameless)
                {
                    writer.Write('\"');
                    writer.Write(member.Name);
                    writer.Write('\"');
                    writer.Write(':');
                }

                var propertyValue = member.Getter(value);
                var childGraph = graph?.GetChildGraph(member.Name);
                ToJson(propertyValue, member.TypeDetail, childGraph, ref writer, nameless);
            }

            if (!nameless)
                writer.Write('}');
            else
                writer.Write(']');
        }
        private static void WriteJsonEnumerable(IEnumerable values, TypeDetail typeDetail, Graph graph, ref CharWriter writer, bool nameless)
        {
            if (typeDetail.CoreType.HasValue)
            {
                ToJsonCoreTypeEnumerabale(values, typeDetail.CoreType.Value, ref writer);
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
                        writer.Write('\"');
                        writer.Write(EnumName.GetName(typeDetail.Type, value));
                        writer.Write('\"');
                    }
                    else
                    {
                        writer.Write("null");
                    }
                }
                return;
            }
            if (typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
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
                        writer.Write('\"');
                        writer.Write(EnumName.GetName(typeDetail.InnerTypes[0], (Enum)value));
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
                var first = true;
                foreach (var value in values)
                {
                    if (first)
                        first = false;
                    else
                        writer.Write(',');
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
                var first = true;
                foreach (var value in values)
                {
                    if (first)
                        first = false;
                    else
                        writer.Write(',');
                    if (value != null)
                    {
                        if (!nameless)
                            writer.Write('{');
                        else
                            writer.Write('[');

                        var firstProperty = true;
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

                            if (firstProperty)
                                firstProperty = false;
                            else
                                writer.Write(',');

                            if (!nameless)
                            {
                                writer.Write('\"');
                                writer.Write(member.Name);
                                writer.Write('\"');
                                writer.Write(':');
                            }

                            var propertyValue = member.Getter(value);
                            var childGraph = graph?.GetChildGraph(member.Name);
                            ToJson(propertyValue, member.TypeDetail, childGraph, ref writer, nameless);
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
        }
        private static void WriteJsonCoreType(object value, CoreType coreType, ref CharWriter writer)
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
        private static void WriteJsonCoreTypeEnumerabale(IEnumerable values, CoreType coreType, ref CharWriter writer)
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

                            ToJsonString(value, ref writer);
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
                            ToJsonChar(value, ref writer);
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
                            writer.Write(value, DateTimeFormat.ISO8601);
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
                            writer.Write(value, DateTimeFormat.ISO8601);
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
                            writer.Write(value, TimeFormat.ISO8601);
                            writer.Write('\"');
                        }
                    }
                    return;
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
        private static void WriteJsonSpecialType(object value, TypeDetail typeDetail, ref CharWriter writer, bool nameless)
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
                            var method = TypeAnalyzer.GetGenericMethodDetail(dictionaryToArrayMethod, typeDetail.InnerTypes[0]);

                            var innerValue = (ICollection)method.Caller(null, new object[] { value });
                            if (!nameless)
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
        private static void WriteJsonSpecialTypeEnumerable(IEnumerable values, TypeDetail typeDetail, ref CharWriter writer, bool nameless)
        {
            var specialType = typeDetail.IsNullable ? typeDetail.InnerTypeDetails[0].SpecialType.Value : typeDetail.SpecialType.Value;
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
                            if (first)
                                first = false;
                            else
                                writer.Write(',');
                            if (value != null)
                            {
                                var innerTypeDetail = typeDetail.InnerTypeDetails[0];

                                var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                                var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").Getter;
                                var method = TypeAnalyzer.GetGenericMethodDetail(dictionaryToArrayMethod, typeDetail.InnerTypes[0]);

                                var innerValue = (ICollection)method.Caller(null, new object[] { value });
                                if (!nameless)
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
        internal static void WriteJsonString(string value, ref CharWriter writer)
        {
            writer.Write('\"');
            if (value == null || value.Length == 0)
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
        private static void WriteJsonChar(char value, ref CharWriter writer)
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