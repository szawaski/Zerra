// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        public static void Serialize(Stream stream, object obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var type = obj.GetType();

            Serialize(stream, obj, type, graph);
        }
        public static void Serialize<T>(Stream stream, T obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var type = typeof(T);

            Serialize(stream, obj, type, graph);
        }
        public static void Serialize(Stream stream, object obj, Type type, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState();
                state.CurrentFrame = CreateWriteFrame(typeDetail, obj, graph);

                for (; ; )
                {
                    WriteConvertBytes(buffer, ref state);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, state.BufferPostion);
#else
                    stream.Write(buffer.AsSpan(0, state.BufferPostion));
#endif

                    if (state.Ended)
                        break;

                    if (state.CharsNeeded == 0)
                        throw new EndOfStreamException("Invalid JSON");

                    if (state.CharsNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.CharsNeeded);

                    state.CharsNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        public static Task SerializeAsync(Stream stream, object obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var type = obj.GetType();

            return SerializeAsync(stream, obj, type, graph);
        }
        public static Task SerializeAsync<T>(Stream stream, T obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var type = typeof(T);

            return SerializeAsync(stream, obj, type, graph);
        }
        public static async Task SerializeAsync(Stream stream, object obj, Type type, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState();
                state.CurrentFrame = CreateWriteFrame(typeDetail, obj, graph);

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

                    if (state.CharsNeeded == 0)
                        throw new EndOfStreamException("Invalid JSON");

                    if (state.CharsNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.CharsNeeded);

                    state.CharsNeeded = 0;
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
                state.BufferPostion = encoding.GetBytes(chars.Slice(0, state.BufferPostion), buffer);
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
                    case WriteFrameType.Null: WriteJsonNull(ref writer, ref state); break;
                    case WriteFrameType.CoreType: WriteJsonCoreType(ref writer, ref state); break;
                    case WriteFrameType.EnumType: WriteJsonEnumType(ref writer, ref state); break;
                    case WriteFrameType.SpecialType: WriteJsonSpecialType(ref writer, ref state); break;
                    case WriteFrameType.ByteArray: WriteJsonByteArray(ref writer, ref state); break;
                    case WriteFrameType.Object: WriteJsonObject(ref writer, ref state); break;

                    case WriteFrameType.CoreTypeEnumerable: WriteJsonCoreTypeEnumerable(ref writer, ref state); break;
                    case WriteFrameType.EnumEnumerable: WriteJsonEnumEnumerable(ref writer, ref state); break;
                    case WriteFrameType.SpecialTypeEnumerable: WriteJsonEnumerable(ref writer, ref state); break;
                    case WriteFrameType.Enumerable: WriteJsonEnumerable(ref writer, ref state); break;
                    case WriteFrameType.ObjectEnumerable: WriteJsonObjectEnumerable(ref writer, ref state); break;
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

        public static void SerializeNameless(Stream stream, object obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            var type = obj.GetType();

            SerializeNameless(stream, obj, type, graph);
        }
        public static void SerializeNameless<T>(Stream stream, T obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            var type = typeof(T);

            SerializeNameless(stream, obj, type, graph);
        }
        public static void SerializeNameless(Stream stream, object obj, Type type, Graph graph = null)
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
                state.Nameless = true;
                state.CurrentFrame = CreateWriteFrame(typeDetail, obj, graph);

                for (; ; )
                {
                    WriteConvertBytes(buffer, ref state);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, state.BufferPostion);
#else
                    stream.Write(buffer.AsSpan(0, state.BufferPostion));
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

        public static Task SerializeNamelessAsync(Stream stream, object obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return Task.CompletedTask;

            var type = obj.GetType();

            return SerializeNamelessAsync(stream, obj, type, graph);
        }
        public static Task SerializeNamelessAsync<T>(Stream stream, T obj, Graph graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return Task.CompletedTask;

            var type = typeof(T);

            return SerializeNamelessAsync(stream, obj, type, graph);
        }
        public static async Task SerializeNamelessAsync(Stream stream, object obj, Type type, Graph graph = null)
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
                state.Nameless = true;
                state.CurrentFrame = CreateWriteFrame(typeDetail, obj, graph);

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

        private static WriteFrame CreateWriteFrame(TypeDetail typeDetail, object obj, Graph? graph = null)
        {
            if (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable && obj != null)
            {
                var objectType = obj.GetType();
                typeDetail = TypeAnalyzer.GetTypeDetail(objectType);
            }

            if (obj == null)
            {
                return new WriteFrame() { FrameType = WriteFrameType.Null, TypeDetail = typeDetail, Object = obj, Graph = graph };
            }

            if (typeDetail.CoreType.HasValue)
            {
                return new WriteFrame() { FrameType = WriteFrameType.CoreType, TypeDetail = typeDetail, Object = obj, Graph = graph };
            }

            if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
            {
                return new WriteFrame() { FrameType = WriteFrameType.EnumType, TypeDetail = typeDetail, Object = obj, Graph = graph };
            }

            if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].SpecialType.HasValue)
            {
                return new WriteFrame() { FrameType = WriteFrameType.SpecialType, TypeDetail = typeDetail, Object = obj, Graph = graph };
            }

            if (typeDetail.IsIEnumerableGeneric)
            {
                if (typeDetail.Type.IsArray && typeDetail.IEnumerableGenericInnerTypeDetails.CoreType == CoreType.Byte)
                {
                    return new WriteFrame() { FrameType = WriteFrameType.ByteArray, TypeDetail = typeDetail, Object = obj, Graph = graph };
                }
                else
                {
                    var elementTypeDetail = typeDetail.InnerTypeDetails[0];
                    if (elementTypeDetail.CoreType.HasValue)
                    {
                        return new WriteFrame() { FrameType = WriteFrameType.CoreTypeEnumerable, TypeDetail = typeDetail, Object = obj, Graph = graph };
                    }
                    if (elementTypeDetail.Type.IsEnum || elementTypeDetail.IsNullable && elementTypeDetail.InnerTypes[0].IsEnum)
                    {
                        return new WriteFrame() { FrameType = WriteFrameType.EnumEnumerable, TypeDetail = typeDetail, Object = obj, Graph = graph };
                    }
                    if (elementTypeDetail.SpecialType.HasValue || elementTypeDetail.IsNullable && elementTypeDetail.InnerTypeDetails[0].SpecialType.HasValue)
                    {
                        return new WriteFrame() { FrameType = WriteFrameType.SpecialTypeEnumerable, TypeDetail = typeDetail, Object = obj, Graph = graph };
                    }
                    if (elementTypeDetail.IsIEnumerableGeneric)
                    {
                        return new WriteFrame() { FrameType = WriteFrameType.Enumerable, TypeDetail = typeDetail, Object = obj, Graph = graph };
                    }
                    return new WriteFrame() { FrameType = WriteFrameType.ObjectEnumerable, TypeDetail = typeDetail, Object = obj, Graph = graph };
                }
            }

            return new WriteFrame() { FrameType = WriteFrameType.Object, TypeDetail = typeDetail, Object = obj, Graph = graph };
        }

        private static void WriteJsonNull(ref CharWriter writer, ref WriteState state)
        {
            if (!writer.TryWrite("null", out var sizeNeeded))
            {
                state.CharsNeeded = sizeNeeded;
                return;
            }
            state.EndFrame();
        }
        private static void WriteJsonCoreType(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            switch (typeDetail.CoreType)
            {
                case CoreType.String:
                    _ = WriteJsonString(ref writer, ref state, (string)state.CurrentFrame.Object, 0);
                    state.EndFrame();
                    return;
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
                    if (!WriteJsonChar(ref writer, ref state, (char)state.CurrentFrame.Object, 0))
                        return;
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

            string str = null;

            if (state.CurrentFrame.State == 0)
            {
                if (typeDetail.IsNullable)
                    str = EnumName.GetName(typeDetail.InnerTypes[0], state.CurrentFrame.Object);
                else
                    str = EnumName.GetName(typeDetail.Type, state.CurrentFrame.Object);

                state.CurrentFrame.Object = str;
                state.CurrentFrame.State = 1;
            }
            if (state.CurrentFrame.State == 1)
            {
                if (!writer.TryWrite('\"', out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.State = 2;
            }
            if (state.CurrentFrame.State == 2)
            {
                str ??= (string)state.CurrentFrame.Object;
                if (!writer.TryWrite(str, out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.State = 3;
            }
            if (state.CurrentFrame.State == 3)
            {
                if (!writer.TryWrite('\"', out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.EndFrame();
            }
        }
        private static void WriteJsonSpecialType(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var specialType = typeDetail.IsNullable ? typeDetail.InnerTypeDetails[0].SpecialType.Value : typeDetail.SpecialType.Value;

            int sizeNeeded;
            switch (specialType)
            {
                case SpecialType.Type:
                    var valueType = state.CurrentFrame.Object == null ? null : (Type)state.CurrentFrame.Object;
                    if (valueType == null)
                    {
                        if (!writer.TryWrite("null", out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                    }
                    _ = WriteJsonString(ref writer, ref state, valueType.FullName, 0);
                    state.EndFrame();
                    return;

                case SpecialType.Dictionary:

                    if (state.CurrentFrame.Object == null)
                    {
                        if (!writer.TryWrite("null", out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                    }

                    var innerTypeDetail = typeDetail.InnerTypeDetails[0];
                    if (state.CurrentFrame.State == 0)
                    {
                        var method = TypeAnalyzer.GetGenericMethodDetail(dictionaryToArrayMethod, typeDetail.InnerTypes[0]);

                        state.CurrentFrame.Enumerator = ((ICollection)method.Caller(null, new object[] { state.CurrentFrame.Object })).GetEnumerator();
                        state.CurrentFrame.State = 1;
                    }

                    if (state.CurrentFrame.State == 1)
                    {
                        if (!state.Nameless)
                        {
                            if (!writer.TryWrite('{', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                        }
                        else
                        {
                            if (!writer.TryWrite('[', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                        }

                        state.CurrentFrame.State = 2;
                    }

                    for (; ; )
                    {
                        switch (state.CurrentFrame.State)
                        {
                            case 2: //Next KeyValuePair
                                if (!state.CurrentFrame.Enumerator.MoveNext())
                                {
                                    state.CurrentFrame.State = 20;
                                    break;
                                }
                                if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                                {
                                    state.CurrentFrame.State = 3;
                                }
                                else
                                {
                                    if (state.Nameless)
                                        state.CurrentFrame.State = 7;
                                    else
                                        state.CurrentFrame.State = 4;
                                    state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                                }
                                break;
                            case 3:  //Seperate Properties
                                if (!writer.TryWrite(',', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                if (state.Nameless)
                                    state.CurrentFrame.State = 7;
                                else
                                    state.CurrentFrame.State = 4;
                                break;

                            case 4: //Key
                                var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                                var key = keyGetter(state.CurrentFrame.Enumerator.Current).ToString();
                                WriteJsonString(ref writer, ref state, key, 4);
                                state.CurrentFrame.State = 5;
                                break;

                            case 5: //KeyValue Seperator
                                if (!writer.TryWrite(':', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 6;
                                break;
                            case 6: //Value
                                var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").Getter;
                                var value = valueGetter(state.CurrentFrame.Enumerator.Current);
                                state.CurrentFrame.State = 2;
                                state.PushFrame(CreateWriteFrame(innerTypeDetail.InnerTypeDetails[1], value));
                                return;

                            case 7:  //Begin Nameless
                                if (!writer.TryWrite('[', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 8;
                                break;
                            case 8: //Nameless Key
                                keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                                key = keyGetter(state.CurrentFrame.Enumerator.Current).ToString();
                                WriteJsonString(ref writer, ref state, key, 8);
                                state.CurrentFrame.State = 9;
                                return;
                            case 9:  //Nameless KeyValue Seperator
                                if (!writer.TryWrite(',', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 10;
                                break;
                            case 10: //Nameless Value
                                valueGetter = innerTypeDetail.GetMemberFieldBacked("value").Getter;
                                value = valueGetter(state.CurrentFrame.Enumerator.Current);
                                state.CurrentFrame.State = 11;
                                state.PushFrame(CreateWriteFrame(innerTypeDetail.InnerTypeDetails[1], value));
                                return;
                            case 11:  //End Nameless
                                if (!writer.TryWrite(']', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 2;
                                break;

                            case 20: //End Dictionary
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

                default:
                    throw new NotImplementedException();
            }
        }
        private static void WriteJsonByteArray(ref CharWriter writer, ref WriteState state)
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

            string str = null;
            if (state.CurrentFrame.State == 1)
            {
                str = Convert.ToBase64String((byte[])state.CurrentFrame.Object);
                state.CurrentFrame.Object = str;
                state.CurrentFrame.State = 2;
            }

            if (state.CurrentFrame.State == 2)
            {
                str ??= (string)state.CurrentFrame.Object;
                if (!writer.TryWrite(str, out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.State = 3;
            }

            if (state.CurrentFrame.State == 3)
            {
                if (!writer.TryWrite('\"', out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
            }
            state.EndFrame();
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
                        else
                        {
                            state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                        }

                        if (state.Nameless)
                            state.CurrentFrame.State = 6;
                        else
                            state.CurrentFrame.State = 3;
                        break;
                    case 2: //Seperate Properties
                        if (!writer.TryWrite(',', out var sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        if (state.Nameless)
                            state.CurrentFrame.State = 6;
                        else
                            state.CurrentFrame.State = 3;
                        break;

                    case 3: //Begin Member Name
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

                        state.CurrentFrame.State = 1;
                        state.PushFrame(CreateWriteFrame(member.TypeDetail, propertyValue, childGraph));
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

        private static void WriteJsonCoreTypeEnumerable(ref CharWriter writer, ref WriteState state)
        {
            int sizeNeeded;
            var typeDetail = state.CurrentFrame.TypeDetail.InnerTypeDetails[0];

            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('[', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

        laststate:
            if (state.CurrentFrame.State == 100)
            {
                if (!writer.TryWrite(']', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.EndFrame();
                return;
            }

            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.CurrentFrame.State = 100;
                        goto laststate;
                    }

                    if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                    {
                        state.CurrentFrame.State = 2;
                    }
                    else
                    {
                        state.CurrentFrame.State = 3;
                        state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                    }
                }
                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(',', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 3;
                }

                switch (typeDetail.CoreType)
                {
                    case CoreType.String:
                        if (!WriteJsonString(ref writer, ref state, (string)state.CurrentFrame.Enumerator.Current, 3))
                            return;
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Boolean:
                        if (!writer.TryWrite((bool)state.CurrentFrame.Enumerator.Current == false ? "false" : "true", out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Byte:
                        if (!writer.TryWrite((byte)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.SByte:
                        if (!writer.TryWrite((sbyte)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Int16:
                        if (!writer.TryWrite((short)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.UInt16:
                        if (!writer.TryWrite((ushort)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Int32:
                        if (!writer.TryWrite((int)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.UInt32:
                        if (!writer.TryWrite((uint)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Int64:
                        if (!writer.TryWrite((long)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.UInt64:
                        if (!writer.TryWrite((ulong)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Single:
                        if (!writer.TryWrite((float)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Double:
                        if (!writer.TryWrite((double)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Decimal:
                        if (!writer.TryWrite((decimal)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Char:
                        if (!WriteJsonChar(ref writer, ref state, (char)state.CurrentFrame.Enumerator.Current, 3))
                            return;
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.DateTime:
                        if (state.CurrentFrame.State == 3)
                        {
                            if (!writer.TryWrite('\"', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 4;
                        }

                        if (state.CurrentFrame.State == 4)
                        {
                            if (!writer.TryWrite((DateTime)state.CurrentFrame.Enumerator.Current, DateTimeFormat.ISO8601, out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 5;
                        }

                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.DateTimeOffset:
                        if (state.CurrentFrame.State == 3)
                        {
                            if (!writer.TryWrite('\"', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 4;
                        }

                        if (state.CurrentFrame.State == 4)
                        {
                            if (!writer.TryWrite((DateTimeOffset)state.CurrentFrame.Enumerator.Current, DateTimeFormat.ISO8601, out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 5;
                        }

                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.TimeSpan:
                        if (state.CurrentFrame.State == 3)
                        {
                            if (!writer.TryWrite('\"', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 4;
                        }

                        if (state.CurrentFrame.State == 4)
                        {
                            if (!writer.TryWrite((TimeSpan)state.CurrentFrame.Enumerator.Current, TimeFormat.ISO8601, out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 5;
                        }

                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;
                    case CoreType.Guid:
                        if (state.CurrentFrame.State == 3)
                        {
                            if (!writer.TryWrite('\"', out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 4;
                        }

                        if (state.CurrentFrame.State == 4)
                        {
                            if (!writer.TryWrite((Guid)state.CurrentFrame.Enumerator.Current, out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return;
                            }
                            state.CurrentFrame.State = 5;
                        }

                        if (!writer.TryWrite('\"', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                        break;

                    case CoreType.BooleanNullable:
                        {
                            var value = (bool?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value == false ? "false" : "true", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.ByteNullable:
                        {
                            var value = (byte?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.SByteNullable:
                        {
                            var value = (sbyte?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.Int16Nullable:
                        {
                            var value = (short?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.UInt16Nullable:
                        {
                            var value = (ushort?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.Int32Nullable:
                        {
                            var value = (int?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.UInt32Nullable:
                        {
                            var value = (uint?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.Int64Nullable:
                        {
                            var value = (long?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.UInt64Nullable:
                        {
                            var value = (ulong?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.SingleNullable:
                        {
                            var value = (float?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.DoubleNullable:
                        {
                            var value = (double?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.DecimalNullable:
                        {
                            var value = (decimal?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (!writer.TryWrite(value.Value, out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.CharNullable:
                        {
                            var value = (char?)state.CurrentFrame.Enumerator.Current; if (value.HasValue)
                            {
                                if (!WriteJsonChar(ref writer, ref state, value.Value, 3))
                                    return;
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.DateTimeNullable:
                        {
                            var value = (DateTime?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (state.CurrentFrame.State == 3)
                                {
                                    if (!writer.TryWrite('\"', out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 4;
                                }

                                if (state.CurrentFrame.State == 4)
                                {
                                    if (!writer.TryWrite(value.Value, DateTimeFormat.ISO8601, out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 5;
                                }

                                if (!writer.TryWrite('\"', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.DateTimeOffsetNullable:
                        {
                            var value = (DateTimeOffset?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (state.CurrentFrame.State == 3)
                                {
                                    if (!writer.TryWrite('\"', out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 4;
                                }

                                if (state.CurrentFrame.State == 4)
                                {
                                    if (!writer.TryWrite(value.Value, DateTimeFormat.ISO8601, out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 5;
                                }

                                if (!writer.TryWrite('\"', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.TimeSpanNullable:
                        {
                            var value = (TimeSpan?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (state.CurrentFrame.State == 3)
                                {
                                    if (!writer.TryWrite('\"', out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 4;
                                }

                                if (state.CurrentFrame.State == 4)
                                {
                                    if (!writer.TryWrite(value.Value, TimeFormat.ISO8601, out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 5;
                                }

                                if (!writer.TryWrite('\"', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }
                    case CoreType.GuidNullable:
                        {
                            var value = (Guid?)state.CurrentFrame.Enumerator.Current;
                            if (value.HasValue)
                            {
                                if (state.CurrentFrame.State == 3)
                                {
                                    if (!writer.TryWrite('\"', out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 4;
                                }

                                if (state.CurrentFrame.State == 4)
                                {
                                    if (!writer.TryWrite(value.Value, out sizeNeeded))
                                    {
                                        state.CharsNeeded = sizeNeeded;
                                        return;
                                    }
                                    state.CurrentFrame.State = 5;
                                }

                                if (!writer.TryWrite('\"', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            else
                            {
                                if (!writer.TryWrite("null", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                            }
                            state.CurrentFrame.State = 1;
                            break;
                        }

                    default:
                        throw new NotImplementedException();
                }
            }
        }
        private static void WriteJsonEnumEnumerable(ref CharWriter writer, ref WriteState state)
        {
            int sizeNeeded;
            var typeDetail = state.CurrentFrame.TypeDetail.InnerTypeDetails[0];

            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('[', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

        laststate:
            if (state.CurrentFrame.State == 100)
            {
                if (!writer.TryWrite(']', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.EndFrame();
                return;
            }

            string str = null;
            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.CurrentFrame.State = 100;
                        goto laststate;
                    }
                    if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                    {
                        state.CurrentFrame.State = 2;
                    }
                    else
                    {
                        state.CurrentFrame.State = 3;
                        state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                    }
                }
                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(',', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 3;
                }
                if (state.CurrentFrame.State == 3)
                {
                    if (typeDetail.IsNullable)
                    {
                        if (state.CurrentFrame.Enumerator.Current == null)
                        {
                            state.CurrentFrame.State = 7;
                        }
                        else
                        {
                            str = EnumName.GetName(typeDetail.InnerTypes[0], state.CurrentFrame.Enumerator.Current);
                            state.CurrentFrame.Object = str;
                            state.CurrentFrame.State = 4;
                        }
                    }
                    else
                    {
                        str = EnumName.GetName(typeDetail.Type, state.CurrentFrame.Enumerator.Current);
                        state.CurrentFrame.Object = str;
                        state.CurrentFrame.State = 4;
                    }
                }
                if (state.CurrentFrame.State == 4)
                {
                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 5;
                }
                if (state.CurrentFrame.State == 5)
                {
                    str ??= (string)state.CurrentFrame.Object;
                    if (!writer.TryWrite(str, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    str = null;
                    state.CurrentFrame.State = 6;
                }
                if (state.CurrentFrame.State == 6)
                {
                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }
                if (state.CurrentFrame.State == 7)
                {
                    if (!writer.TryWrite("null", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }
            }
        }
        private static void WriteJsonEnumerable(ref CharWriter writer, ref WriteState state)
        {
            int sizeNeeded;
            var typeDetail = state.CurrentFrame.TypeDetail.InnerTypeDetails[0];

            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('[', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

        laststate:
            if (state.CurrentFrame.State == 100)
            {
                if (!writer.TryWrite(']', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.EndFrame();
                return;
            }

            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.CurrentFrame.State = 100;
                        goto laststate;
                    }
                    if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                    {
                        state.CurrentFrame.State = 2;
                    }
                    else
                    {
                        state.CurrentFrame.State = 3;
                        state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                    }
                }
                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(',', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 3;
                }
                if (state.CurrentFrame.State == 3)
                {
                    state.CurrentFrame.State = 1;
                    state.PushFrame(CreateWriteFrame(typeDetail, state.CurrentFrame.Enumerator.Current));
                    return;
                }
            }
        }
        private static void WriteJsonObjectEnumerable(ref CharWriter writer, ref WriteState state)
        {
            int sizeNeeded;
            var typeDetail = state.CurrentFrame.TypeDetail.InnerTypeDetails[0];

            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('[', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

        laststate:
            if (state.CurrentFrame.State == 100)
            {
                if (!writer.TryWrite(']', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.EndFrame();
                return;
            }

            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.CurrentFrame.State = 100;
                        goto laststate;
                    }

                    if (state.CurrentFrame.Enumerator.Current == null)
                    {
                        if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                        {
                            state.CurrentFrame.State = 2;
                        }
                        else
                        {
                            state.CurrentFrame.State = 3;
                            state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                        }
                    }
                    else
                    {
                        if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                        {
                            state.CurrentFrame.State = 4;
                        }
                        else
                        {
                            state.CurrentFrame.State = 5;
                            state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                        }
                    }
                    state.CurrentFrame.MemberEnumerator = typeDetail.SerializableMemberDetails.GetEnumerator();
                }

                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(",null", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }
                if (state.CurrentFrame.State == 3)
                {
                    if (!writer.TryWrite("null", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }

                if (state.CurrentFrame.State == 4)
                {
                    if (!writer.TryWrite(",", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 5;
                }

                if (state.CurrentFrame.State == 5)
                {
                    if (!state.Nameless)
                    {
                        if (!writer.TryWrite('{', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 6;
                    }
                    else
                    {
                        if (!writer.TryWrite('[', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 6;
                    }
                }
                if (state.CurrentFrame.State == 6)
                {
                nextprop:
                    if (!state.CurrentFrame.MemberEnumerator.MoveNext())
                    {
                        state.CurrentFrame.State = 99;
                    }
                    else
                    {
                        var graph = state.CurrentFrame.Graph;
                        var member = state.CurrentFrame.MemberEnumerator.Current;
                        if (graph != null)
                        {
                            if (member.TypeDetail.IsGraphLocalProperty)
                            {
                                if (!graph.HasLocalProperty(member.Name))
                                    goto nextprop;
                            }
                            else
                            {
                                if (!graph.HasChildGraph(member.Name))
                                    goto nextprop;
                            }
                        }

                        if (state.Nameless)
                            state.CurrentFrame.State = 20;
                        else
                            state.CurrentFrame.State = 7;
                    }
                }

                if (state.CurrentFrame.State == 7)
                {
                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 8;
                }
                if (state.CurrentFrame.State == 8)
                {
                    if (!writer.TryWrite(state.CurrentFrame.MemberEnumerator.Current.Name, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 9;
                }
                if (state.CurrentFrame.State == 9)
                {
                    if (!writer.TryWrite("\":", out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 20;
                }

                if (state.CurrentFrame.State == 20)
                {
                    var graph = state.CurrentFrame.Graph;
                    var member = state.CurrentFrame.MemberEnumerator.Current;
                    var propertyValue = member.Getter(state.CurrentFrame.Enumerator.Current);
                    var childGraph = graph?.GetChildGraph(member.Name);
                    state.CurrentFrame.State = 6;
                    state.PushFrame(CreateWriteFrame(member.TypeDetail, propertyValue, childGraph));
                    return;
                }

                if (state.CurrentFrame.State == 99)
                {
                    if (!state.Nameless)
                    {
                        if (!writer.TryWrite('}', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }
                    else
                    {
                        if (!writer.TryWrite(']', out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool WriteJsonString(ref CharWriter writer, ref WriteState state, string value, byte initialFrameState)
        {
            int sizeNeeded;

            if (value == null)
            {
                if (!writer.TryWrite("null", out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                return true;
            }

            if (state.CurrentFrame.State == initialFrameState + 0)
            {
                if (!writer.TryWrite('\"', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 1);
            }

            if (state.CurrentFrame.State == initialFrameState + 1)
            {
                if (value.Length == 0)
                {
                    if (!writer.TryWrite('\"', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                }
                state.WorkingString = value.AsMemory();
                state.CurrentFrame.State = (byte)(initialFrameState + 2);
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

                        if (state.CurrentFrame.State == initialFrameState + 2)
                        {
                            var slice = chars.Slice(state.WorkingStringStart, state.WorkingStringIndex - state.WorkingStringStart);
                            if (!writer.TryWrite(slice, out sizeNeeded))
                            {
                                state.CharsNeeded = sizeNeeded;
                                return false;
                            }
                            state.CurrentFrame.State = (byte)(initialFrameState + 3);
                        }

                        var code = lowUnicodeIntToEncodedHex[c];
                        if (!writer.TryWrite(code, out sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return false;
                        }
                        state.CurrentFrame.State = (byte)(initialFrameState + 2);
                        state.WorkingStringStart = state.WorkingStringIndex + 1;
                        continue;
                }

                if (state.CurrentFrame.State == initialFrameState + 2)
                {
                    var slice = chars.Slice(state.WorkingStringStart, state.WorkingStringIndex - state.WorkingStringStart);
                    if (!writer.TryWrite(slice, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    state.CurrentFrame.State = (byte)(initialFrameState + 3);
                }
                if (state.CurrentFrame.State == initialFrameState + 3)
                {
                    if (!writer.TryWrite('\\', out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                    state.CurrentFrame.State = (byte)(initialFrameState + 4);
                }
                if (!writer.TryWrite(escapedChar, out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 2);
                state.WorkingStringStart = state.WorkingStringIndex + 1;
            }

            if (state.CurrentFrame.State == initialFrameState + 2)
            {
                if (state.WorkingStringStart != chars.Length)
                {
                    var slice = chars.Slice(state.WorkingStringStart, chars.Length - state.WorkingStringStart);
                    if (!writer.TryWrite(slice, out sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return false;
                    }
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 3);
            }

            if (!writer.TryWrite('\"', out sizeNeeded))
            {
                state.CharsNeeded = sizeNeeded;
                return false;
            }

            state.WorkingStringIndex = 0;
            state.WorkingStringStart = 0;
            state.WorkingString = null;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool WriteJsonChar(ref CharWriter writer, ref WriteState state, char c, byte initialFrameState)
        {
            int sizeNeeded;

            if (state.CurrentFrame.State == initialFrameState + 0)
            {
                switch (c)
                {
                    case '\\':
                        if (!writer.TryWrite("\"\\\\\"", out sizeNeeded))
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
                    state.CurrentFrame.State = (byte)(initialFrameState + 1);
                else
                    state.CurrentFrame.State = (byte)(initialFrameState + 3);
            }

            if (state.CurrentFrame.State == initialFrameState + 1)
            {
                if (!writer.TryWrite('\"', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 2);
            }
            if (state.CurrentFrame.State == initialFrameState + 2)
            {
                var code = lowUnicodeIntToEncodedHex[c];
                if (!writer.TryWrite(code, out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 10);
            }

            if (state.CurrentFrame.State == initialFrameState + 3)
            {
                if (!writer.TryWrite('\"', out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 4);
            }
            if (state.CurrentFrame.State == initialFrameState + 4)
            {
                if (!writer.TryWrite(c, out sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.State = (byte)(initialFrameState + 10);
            }

            if (!writer.TryWrite('\"', out sizeNeeded))
            {
                state.CharsNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}