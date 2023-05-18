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
                state.CurrentFrame = new WriteFrame() { TypeDetail = typeDetail, Object = obj, FrameType = WriteFrameType.Value };

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
                    case WriteFrameType.Value: WriteJson(ref writer, ref state); break;
                    case WriteFrameType.CoreType: WriteJsonCoreType(ref writer, ref state); break;
                    case WriteFrameType.EnumType: WriteJsonEnumType(ref writer, ref state); break;
                    case WriteFrameType.SpecialType: WriteJsonSpecialType(ref writer, ref state); break;
                    case WriteFrameType.ByteArray: WriteJsonByteArray(ref writer, ref state); break;
                    case WriteFrameType.Object: WriteJsonObject(ref writer, ref state); break;

                    case WriteFrameType.CoreTypeEnumerable: WriteJsonCoreTypeEnumerable(ref writer, ref state); break;
                    case WriteFrameType.EnumEnumerable: WriteJsonEnumEnumerable(ref writer, ref state); break;
                    case WriteFrameType.SpecialTypeEnumerable: WriteJsonSpecialTypeEnumerable(ref writer, ref state); break;
                    case WriteFrameType.GenericEnumerable: WriteJsonGenericEnumerable(ref writer, ref state); break;
                    case WriteFrameType.Enumerable: WriteJsonEnumerable(ref writer, ref state); break;
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
                state.CurrentFrame = new WriteFrame() { TypeDetail = typeDetail, Object = obj, FrameType = WriteFrameType.Value };

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
                if (typeDetail.Type.IsArray && typeDetail.IEnumerableGenericInnerTypeDetails.CoreType == CoreType.Byte)
                {
                    state.CurrentFrame.FrameType = WriteFrameType.ByteArray;
                    return;
                }
                else
                {
                    if (typeDetail.CoreType.HasValue)
                    {
                        state.CurrentFrame.FrameType = WriteFrameType.CoreTypeEnumerable;
                        return;
                    }
                    if (typeDetail.Type.IsEnum || typeDetail.IsNullable && typeDetail.InnerTypes[0].IsEnum)
                    {
                        state.CurrentFrame.FrameType = WriteFrameType.EnumEnumerable;
                        return;
                    }
                    if (typeDetail.SpecialType.HasValue || typeDetail.IsNullable && typeDetail.InnerTypeDetails[0].SpecialType.HasValue)
                    {
                        state.CurrentFrame.FrameType = WriteFrameType.SpecialTypeEnumerable;
                        return;
                    }
                    if (typeDetail.IsIEnumerableGeneric)
                    {
                        state.CurrentFrame.FrameType = WriteFrameType.GenericEnumerable;
                        return;
                    }
                    state.CurrentFrame.FrameType = WriteFrameType.Enumerable;
                    return;
                }
            }

            state.CurrentFrame.FrameType = WriteFrameType.Object;
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

            string str = null;

            if (state.CurrentFrame.State == 0)
            {
                if (!writer.TryWrite('\"', out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }

                if (typeDetail.IsNullable)
                {
                    if (state.CurrentFrame.Object == null)
                        str = "null";
                    else
                        EnumName.GetName(typeDetail.InnerTypes[0], state.CurrentFrame.Object);
                }
                else
                {
                    str = EnumName.GetName(typeDetail.Type, state.CurrentFrame.Object);
                }

                state.CurrentFrame.Object = str;
                state.CurrentFrame.State = 1;
            }
            if (state.CurrentFrame.State == 1)
            {
                str ??= (string)state.CurrentFrame.Object;
                if (!writer.TryWrite(str, out var sizeNeeded))
                {
                    state.CharsNeeded = sizeNeeded;
                    return;
                }
                state.CurrentFrame.State = 2;
            }
            if (state.CurrentFrame.State == 2)
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
                    state.CurrentFrame.FrameType = WriteFrameType.String;
                    state.CurrentFrame.Object = valueType.FullName;
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
                                    state.CurrentFrame.State = 2;
                                    break;
                                }
                                else
                                {
                                    state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                                }

                                if (state.Nameless)
                                    state.CurrentFrame.State = 7;
                                else
                                    state.CurrentFrame.State = 4;
                                break;
                            case 3:  //Seperate Properties
                                if (!writer.TryWrite(',', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                break;

                            case 4: //Key
                                state.CurrentFrame.State = 5;
                                var keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                                var key = keyGetter(state.CurrentFrame.Enumerator.Current);
                                state.PushFrame(new WriteFrame() { TypeDetail = innerTypeDetail.InnerTypeDetails[0], Object = key, FrameType = WriteFrameType.Value });
                                break;
                            case 5: //KeyValue Seperator
                                if (!writer.TryWrite("\":", out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 6;
                                break;
                            case 6: //Value
                                state.CurrentFrame.State = 1;
                                var valueGetter = innerTypeDetail.GetMemberFieldBacked("value").Getter;
                                var value = valueGetter(state.CurrentFrame.Enumerator.Current);
                                state.PushFrame(new WriteFrame() { TypeDetail = innerTypeDetail.InnerTypeDetails[1], Object = value, FrameType = WriteFrameType.Value });
                                break;

                            case 7:  //Begin Nameless
                                if (!writer.TryWrite('[', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 8;
                                break;
                            case 8: //Nameless Key
                                state.CurrentFrame.State = 9;
                                keyGetter = innerTypeDetail.GetMemberFieldBacked("key").Getter;
                                key = keyGetter(state.CurrentFrame.Enumerator.Current);
                                state.PushFrame(new WriteFrame() { TypeDetail = innerTypeDetail.InnerTypeDetails[0], Object = key, FrameType = WriteFrameType.Value });
                                break;
                            case 9:  //Nameless KeyValue Seperator
                                if (!writer.TryWrite(',', out sizeNeeded))
                                {
                                    state.CharsNeeded = sizeNeeded;
                                    return;
                                }
                                state.CurrentFrame.State = 10;
                                break;
                            case 10:  //End Nameless
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

        private static void WriteJsonCoreTypeEnumerable(ref CharWriter writer, ref WriteState state)
        {

        }
        private static void WriteJsonEnumEnumerable(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            if (state.CurrentFrame.State == 0)
            {
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

            string str = null;
            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.EndFrame();
                        return;
                    }
                    if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                        state.CurrentFrame.State = 2;
                    else
                        state.CurrentFrame.State = 3;
                    state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                }
                if (state.CurrentFrame.State == 2)
                {
                    if (writer.TryWrite(',', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 3;
                }
                if (state.CurrentFrame.State == 3)
                {
                    if (writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }

                    if (typeDetail.IsNullable)
                    {
                        if (state.CurrentFrame.Object == null)
                            str = "null";
                        else
                            EnumName.GetName(typeDetail.InnerTypes[0], state.CurrentFrame.Object);
                    }
                    else
                    {
                        str = EnumName.GetName(typeDetail.Type, state.CurrentFrame.Object);
                    }
                    state.CurrentFrame.Object = str;
                    state.CurrentFrame.State = 4;
                }
                if (state.CurrentFrame.State == 4)
                {
                    str ??= (string)state.CurrentFrame.Object;
                    if (writer.TryWrite(str, out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    str = null;
                    state.CurrentFrame.State = 5;
                }
                if (state.CurrentFrame.State == 5)
                {
                    if (writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }
            }
        }
        private static void WriteJsonSpecialTypeEnumerable(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var specialTypeDetail = typeDetail.IsNullable ? typeDetail.InnerTypeDetails[0] : typeDetail;

            if (state.CurrentFrame.State == 0)
            {
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.EndFrame();
                        return;
                    }
                    state.CurrentFrame.State = 2;
                    if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                        state.CurrentFrame.State = 2;
                    else
                        state.CurrentFrame.State = 3;
                    state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                }
                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(',', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 3;
                }
                if (state.CurrentFrame.State == 4)
                {
                    state.PushFrame(new WriteFrame() { TypeDetail = specialTypeDetail, Object = state.CurrentFrame.Enumerator.Current, FrameType = WriteFrameType.SpecialType });
                    state.CurrentFrame.State = 5;
                    return;
                }
            }
        }
        private static void WriteJsonGenericEnumerable(ref CharWriter writer, ref WriteState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            if (state.CurrentFrame.State == 0)
            {
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.EndFrame();
                        return;
                    }
                    state.CurrentFrame.State = 2;
                    if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                        state.CurrentFrame.State = 2;
                    else
                        state.CurrentFrame.State = 3;
                    state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                }
                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(',', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 3;
                }
                if (state.CurrentFrame.State == 3)
                {
                    if (!writer.TryWrite('[', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 4;
                }
                if (state.CurrentFrame.State == 4)
                {
                    state.PushFrame(new WriteFrame() { TypeDetail = typeDetail.InnerTypeDetails[0], Object = state.CurrentFrame.Enumerator.Current, FrameType = WriteFrameType.Value });
                    state.CurrentFrame.State = 5;
                }
                if (state.CurrentFrame.State == 5)
                {
                    if (!writer.TryWrite(']', out var sizeNeeded))
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
            var typeDetail = state.CurrentFrame.TypeDetail;

            if (state.CurrentFrame.State == 0)
            {
                state.CurrentFrame.Enumerator = ((IEnumerable)state.CurrentFrame.Object).GetEnumerator();
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                if (state.CurrentFrame.State == 1)
                {
                    if (!state.CurrentFrame.Enumerator.MoveNext())
                    {
                        state.EndFrame();
                        return;
                    }
                    state.CurrentFrame.MemberEnumerator = typeDetail.SerializableMemberDetails.GetEnumerator();
                    if (state.CurrentFrame.Enumerator.Current == null)
                    {
                        if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                            state.CurrentFrame.State = 2;
                        else
                            state.CurrentFrame.State = 3;
                    }
                    else
                    {
                        if (state.CurrentFrame.EnumeratorPassedFirstProperty)
                            state.CurrentFrame.State = 4;
                        else
                            state.CurrentFrame.State = 5;
                    }
                    state.CurrentFrame.EnumeratorPassedFirstProperty = true;
                }

                if (state.CurrentFrame.State == 2)
                {
                    if (!writer.TryWrite(",null", out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }
                if (state.CurrentFrame.State == 3)
                {
                    if (!writer.TryWrite("null", out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 1;
                }

                if (state.CurrentFrame.State == 4)
                {
                    if (!writer.TryWrite(",", out var sizeNeeded))
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
                        if (!writer.TryWrite('{', out var sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 6;
                    }
                    else
                    {
                        if (!writer.TryWrite('[', out var sizeNeeded))
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
                        state.CurrentFrame.State = 100;
                    }

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

                    state.CurrentFrame.State = 7;
                }
                if (state.CurrentFrame.State == 7)
                {
                    if (!state.CurrentFrame.MemberEnumerator.MoveNext())
                    {
                        state.CurrentFrame.State = 100;
                    }
                    if (state.Nameless)
                        state.CurrentFrame.State = 20;
                    else
                        state.CurrentFrame.State = 8;
                }

                if (state.CurrentFrame.State == 8)
                {
                    if (!writer.TryWrite('\"', out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 9;
                }
                if (state.CurrentFrame.State == 9)
                {
                    if (!writer.TryWrite(state.CurrentFrame.MemberEnumerator.Current.Name, out var sizeNeeded))
                    {
                        state.CharsNeeded = sizeNeeded;
                        return;
                    }
                    state.CurrentFrame.State = 10;
                }
                if (state.CurrentFrame.State == 10)
                {
                    if (!writer.TryWrite("\":", out var sizeNeeded))
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
                    var propertyValue = member.Getter(state.CurrentFrame.Object);
                    var childGraph = graph?.GetChildGraph(member.Name);
                    state.PushFrame(new WriteFrame() { TypeDetail = member.TypeDetail, Object = propertyValue, Graph = childGraph, FrameType = WriteFrameType.Value });
                    state.CurrentFrame.State = 1;
                }

                if (state.CurrentFrame.State == 100)
                {
                    if (!state.Nameless)
                    {
                        if (!writer.TryWrite('}', out var sizeNeeded))
                        {
                            state.CharsNeeded = sizeNeeded;
                            return;
                        }
                        state.CurrentFrame.State = 1;
                    }
                    else
                    {
                        if (!writer.TryWrite(']', out var sizeNeeded))
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
            int sizeNeeded;
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

            if (!writer.TryWrite('\"', out sizeNeeded))
            {
                state.CharsNeeded = sizeNeeded;
                return false;
            }
            return true;
        }
    }
}