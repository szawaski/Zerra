// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
                state.CurrentFrame = new WriteFrame() { TypeDetail = typeDetail, Object = obj, FrameType = ReadFrameType.Value };

                for (; ; )
                {
                    WriteConvertBytes(buffer, ref state);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, state.BufferPostion);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, state.BufferPostion));
#endif

                    if (state.Ended)
                        break;

                    if (state.CharsNeeded > 0)
                    {
                        if (state.CharsNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.CharsNeeded);

                        state.CharsNeeded = 0;
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
        private static void WriteConvertBytes(Span<byte> buffer, ref WriteState state)
        {
            var bufferCharOwner = BufferArrayPool<char>.Rent(buffer.Length);

            try
            {

#if NET5_0_OR_GREATER
                var chars = bufferCharOwner.AsSpan().Slice(0, buffer.Length);
                Write(chars, ref state);
                _ = encoding.GetBytes(chars, buffer);
#else
                Write(bufferCharOwner.AsSpan().Slice(0, buffer.Length), ref state);
                _ = encoding.GetBytes(bufferCharOwner, 0, state.BufferPostion, buffer.ToArray(), 0);
#endif
            }
            finally
            {
                Array.Clear(bufferCharOwner, 0, bufferCharOwner.Length);
                BufferArrayPool<char>.Return(bufferCharOwner);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Write(Span<char> buffer, ref WriteState state)
        {
            var writer = new CharWriter(buffer);
            for (; ; )
            {
                switch (state.CurrentFrame.FrameType)
                {
                    case ReadFrameType.Value: WriteJson(ref writer, ref state); break;
                    case ReadFrameType.CoreType: WriteCoreType(ref writer, ref state); break;
                }
                if (state.Ended)
                {
                    state.BufferPostion = writer.Position;
                    return;
                }
                if (state.CharsNeeded > 0)
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

        private static void WriteJson(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            if (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable && state.CurrentFrame.Object != null)
            {
                var objectType = state.CurrentFrame.Object.GetType();
                typeDetail = TypeAnalyzer.GetTypeDetail(objectType);
            }

            if (state.CurrentFrame.Object == null)
            {
                if (!writer.TryWrite("null", out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.EndFrame();
                return;
            }

            if (typeDetail.CoreType.HasValue)
            {
                if (typeDetail.CoreType == CoreType.String)
                    state.CurrentFrame.FrameType = WriteFrameType.String;
                else
                    state.CurrentFrame.FrameType = WriteFrameType.CoreType;
                return;
            }

            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                state.CurrentFrame.FrameType = WriteFrameType.EnumType;
                return;
            }

            if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].SpecialType.HasValue)
            {
                state.CurrentFrame.FrameType = WriteFrameType.SpecialType;
                return;
            }

            if (typeDetail.IsIEnumerableGeneric)
            {
                state.CurrentFrame.FrameType = WriteFrameType.ByteArray;
                return;
            }

            state.CurrentFrame.FrameType = WriteFrameType.Object;
        }
        private static void WriteJsonEnumerable(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

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
        private static void WriteJsonCoreType(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            switch (typeDetail.CoreType)
            {
                case CoreType.Boolean:
                case CoreType.BooleanNullable:
                    if (!writer.TryWrite((bool)state.CurrentFrame.Object == false ? "false" : "true", out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Byte:
                case CoreType.ByteNullable:
                    if (!writer.TryWrite((byte)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.SByte:
                case CoreType.SByteNullable:
                    if (!writer.TryWrite((sbyte)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Int16:
                case CoreType.Int16Nullable:
                    if (!writer.TryWrite((short)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.UInt16:
                case CoreType.UInt16Nullable:
                    if (!writer.TryWrite((ushort)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Int32:
                case CoreType.Int32Nullable:
                    if (!writer.TryWrite((int)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.UInt32:
                case CoreType.UInt32Nullable:
                    if (!writer.TryWrite((uint)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Int64:
                case CoreType.Int64Nullable:
                    if (!writer.TryWrite((long)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.UInt64:
                case CoreType.UInt64Nullable:
                    if (!writer.TryWrite((ulong)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Single:
                case CoreType.SingleNullable:
                    if (!writer.TryWrite((float)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Double:
                case CoreType.DoubleNullable:
                    if (!writer.TryWrite((double)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Decimal:
                case CoreType.DecimalNullable:
                    if (!writer.TryWrite((decimal)state.CurrentFrame.Object, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Char:
                case CoreType.CharNullable:
                    if (WriteJsonChar(ref writer, ref state))
                        state.EndFrame();
                    return;
                case CoreType.DateTime:
                case CoreType.DateTimeNullable:
                    if (state.CurrentFrame.State == 0)
                    {
                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }

                    if (state.CurrentFrame.State == 1)
                    {
                        if (!writer.TryWrite((DateTime)state.CurrentFrame.Object, DateTimeFormat.ISO8601, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 2;
                    }

                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.DateTimeOffset:
                case CoreType.DateTimeOffsetNullable:
                    if (state.CurrentFrame.State == 0)
                    {
                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }

                    if (state.CurrentFrame.State == 1)
                    {
                        if (!writer.TryWrite((DateTimeOffset)state.CurrentFrame.Object, DateTimeFormat.ISO8601, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 2;
                    }

                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.TimeSpan:
                case CoreType.TimeSpanNullable:
                    if (state.CurrentFrame.State == 0)
                    {
                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }

                    if (state.CurrentFrame.State == 1)
                    {
                        if (!writer.TryWrite((TimeSpan)state.CurrentFrame.Object, TimeFormat.ISO8601, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 2;
                    }

                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;
                case CoreType.Guid:
                case CoreType.GuidNullable:
                    if (state.CurrentFrame.State == 0)
                    {
                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }

                    if (state.CurrentFrame.State == 1)
                    {
                        if (!writer.TryWrite((Guid)state.CurrentFrame.Object, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 2;
                    }

                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                    return;

                default:
                    throw new NotImplementedException();
            }
        }
        private static void WriteJsonEnumType(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            if (typeDetail.CoreType.HasValue)
            {
                state.CurrentFrame.FrameType = WriteFrameType.CoreType;
                return;
            }

            if (typeDetail.Type.IsEnum)
            {
                if (state.CurrentFrame.State == 0)
                {
                    if (!writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }
                if (state.CurrentFrame.State == 1)
                {
                    if (!writer.TryWrite(EnumName.GetName(typeDetail.Type, state.CurrentFrame.Object), out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 2;
                }

                {
                    if (!writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                }
            }
        }
        private static void WriteJsonByteArray(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            var innerTypeDetails = typeDetail.IEnumerableGenericInnerTypeDetails;
            if (typeDetail.Type.IsArray && innerTypeDetails.CoreType == CoreType.Byte)
            {
                //special case
                if (state.CurrentFrame.State == 0)
                {
                    if (!writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }

                if (state.CurrentFrame.State == 1)
                {
                    var str = Convert.ToBase64String((byte[])state.CurrentFrame.Object);
                    if (!writer.TryWrite(str, out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 2;
                }

                if (state.CurrentFrame.State == 2)
                {
                    if (state.CurrentFrame.State == 2 && !writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                }
                state.EndFrame();
                return;
            }
            else
            {
                state.CurrentFrame.FrameType = WriteFrameType.Enumerable;
                return;
            }
        }
        private static void WriteJsonObject(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var graph = state.CurrentFrame.Graph;

            if (state.CurrentFrame.State == 0)
            {
                if (!state.Nameless)
                {
                    if (!writer.TryWrite('{', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                }
                else
                {
                    if (!writer.TryWrite('[', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                }

                state.CurrentFrame.MemberEnumerator = typeDetail.SerializableMemberDetails.GetEnumerator();
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //Next Property
                        if (!state.CurrentFrame.MemberEnumerator.MoveNext())
                        {
                            state.CurrentFrame.State = 7;
                            break;
                        }
                        if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                        {
                            state.CurrentFrame.State = 2;
                            break;
                        }

                        state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                        state.CurrentFrame.State = 3;
                        break;
                    case 2: //Seperate Properties
                        if (!writer.TryWrite(',', out var sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        if (state.Nameless)
                            state.CurrentFrame.State = 10;
                        else
                            state.CurrentFrame.State = 3;
                        break;

                    case 3: //Being Member Name
                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 4;
                        break;
                    case 4: //Member Name
                        var member = state.CurrentFrame.MemberEnumerator.Current;
                        if (!writer.TryWrite(member.Name, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 5;
                        break;
                    case 5: //End Member Name
                        if (!writer.TryWrite("\":", out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 6;
                        break;


                    case 6: //Member Value
                        member = state.CurrentFrame.MemberEnumerator.Current;
                        var propertyValue = member.Getter(state.CurrentFrame.Object);
                        var childGraph = graph?.GetChildGraph(member.Name);

                        state.PushFrame(new WriteFrame() { TypeDetail = member.TypeDetail, Object = propertyValue, Graph = childGraph });
                        state.CurrentFrame.State = 1;
                        return;

                    case 7: //End Object
                        if (!state.Nameless)
                        {
                            if (!writer.TryWrite('}', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                        }
                        else
                        {
                            if (!writer.TryWrite(']', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                        }
                        state.EndFrame();
                        return;
                }
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
        private static void WriteJsonString(ref CharWriter writer, ref WriteState state)
        {
            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('\"', out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.State = 1;
            }

            if (state.CurrentFrame.State == 1)
            {
                if (state.CurrentFrame.Object == null)
                {
                    if (!writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                }

                var value = (string)state.CurrentFrame.Object;
                if (value.Length == 0)
                {
                    if (!writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.EndFrame();
                }

                state.CurrentFrame.State = 2;
                state.WorkingString = value.AsMemory();
            }

            var chars = state.WorkingString.Span;

            for (; state.WorkingStringIndex < chars.Length; state.WorkingStringIndex++)
            {
                var c = chars[state.WorkingStringIndex];
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

                        writer.Write(chars.Slice(state.WorkingStringMark, state.WorkingStringIndex - state.WorkingStringMark));
                        state.WorkingStringMark = state.WorkingStringIndex + 1;
                        var code = lowUnicodeIntToEncodedHex[c];
                        writer.Write(code);
                        continue;
                }

                writer.Write('\\');
                writer.Write(escapedChar);
            }

            if (state.WorkingStringMark != chars.Length)
                writer.Write(chars.Slice(state.WorkingStringMark, chars.Length - state.WorkingStringMark));
            writer.Write('\"');

            state.WorkingString = null;
            state.WorkingStringIndex = 0;
            state.WorkingStringMark = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool WriteJsonChar(ref CharWriter writer, ref WriteState state)
        {
            var c = (char)state.CurrentFrame.Object;
            switch (c)
            {
                case '\\':
                    if (!writer.TryWrite("\"\\\\\"", out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '"':
                    if (!writer.TryWrite("\"\\\"\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '/':
                    if (!writer.TryWrite("\"\\/\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '\b':
                    if (!writer.TryWrite("\"\\b\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '\t':
                    if (!writer.TryWrite("\"\\t\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '\n':
                    if (!writer.TryWrite("\"\\n\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '\f':
                    if (!writer.TryWrite("\"\\f\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                case '\r':
                    if (!writer.TryWrite("\"\\r\"", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
            }

            if (c < ' ')
            {
                var code = lowUnicodeIntToEncodedHex[c];
                if (state.CurrentFrame.State == 0)
                {
                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    state.CurrentFrame.State = 1;
                }

                if (state.CurrentFrame.State == 1)
                {
                    if (!writer.TryWrite(code, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    state.CurrentFrame.State = 2;
                }

                if (!writer.TryWrite('\"', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                return true;
            }

            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('\"', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = 1;
            }

            if (state.CurrentFrame.State == 1)
            {
                if (!writer.TryWrite(c, out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = 2;
            }

            if (!writer.TryWrite('\"', out var sizeNeeded))
            {
                state.CharsNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}