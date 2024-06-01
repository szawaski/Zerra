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

namespace Zerra.Serialization.Json
{
    public static partial class JsonSerializer
    {
        public static T? DeserializeStackBased<T>(Memory<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            return (T?)DeserializeStackBased(typeof(T), bytes, options, graph);
        }
        public static object? DeserializeStackBased(Type type, Memory<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
                var state = new ReadState()
                {
                    Nameless = options.Nameless,

                    CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value }
                };

                ReadConvertBytes(bytes.Span, ref state, ref decodeBuffer, ref decodeBufferPosition);

                if (!state.Ended || state.CharsNeeded > 0)
                    throw new EndOfStreamException();

                return state.LastFrameResultObject;
            }
            finally
            {
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }

        public static T? DeserializeStackBased<T>(Memory<char> json, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            return (T?)DeserializeStackBased(typeof(T), json, options, graph);
        }
        public static object? DeserializeStackBased(Type type, Memory<char> json, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
                var state = new ReadState()
                {
                    Nameless = options.Nameless,

                    CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value }
                };

                Read(json.Span, ref state, ref decodeBuffer, ref decodeBufferPosition);

                if (!state.Ended || state.CharsNeeded > 0)
                    throw new EndOfStreamException();

                return state.LastFrameResultObject;
            }
            finally
            {
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }

        public static T? Deserialize<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            return (T?)Deserialize(typeof(T), stream, options, graph);
        }
        public static object? Deserialize(Type type, Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
                var isFinalBlock = false;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = stream.Read(buffer, position, buffer.Length - position);
#else
                    read = stream.Read(buffer.AsSpan(position));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    position += read;
                    length = position;
                }

                if (position == 0)
                {
                    if (typeDetail.CoreType == CoreType.String)
                        return (object)String.Empty;
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,

                    IsFinalBlock = isFinalBlock,
                    CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value, Graph = graph }
                };

                for (; ; )
                {
                    var usedBytes = ReadConvertBytes(buffer.AsSpan().Slice(0, length), ref state, ref decodeBuffer, ref decodeBufferPosition);

                    if (state.Ended)
                        break;

                    if (state.CharsNeeded > 0)
                    {
                        if (state.IsFinalBlock)
                            throw new EndOfStreamException();

                        Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                        position = length - usedBytes;

                        if (position + state.CharsNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, position + state.CharsNeeded);

                        while (position < buffer.Length)
                        {
#if NETSTANDARD2_0
                            read = stream.Read(buffer, position, buffer.Length - position);
#else
                            read = stream.Read(buffer.AsSpan(position));
#endif
                            if (read == 0)
                            {
                                state.IsFinalBlock = true;
                                break;
                            }
                            position += read;
                            length = position;
                        }

                        if (position < state.CharsNeeded)
                            throw new EndOfStreamException();

                        state.CharsNeeded = 0;
                    }
                }

                return state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }

        public static async Task<T?> DeserializeAsync<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var type = typeof(T);

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
                var isFinalBlock = false;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(position));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    position += read;
                    length = position;
                }

                if (position == 0)
                {
                    if (typeDetail.CoreType == CoreType.String)
                        return (T)(object)String.Empty; //TODO better way to convert type???
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,

                    IsFinalBlock = isFinalBlock,
                    CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value, Graph = graph }
                };

                for (; ; )
                {
                    var usedBytes = ReadConvertBytes(buffer.AsSpan().Slice(0, length), ref state, ref decodeBuffer, ref decodeBufferPosition);

                    if (state.Ended)
                        break;

                    if (state.CharsNeeded > 0)
                    {
                        if (state.IsFinalBlock)
                            throw new EndOfStreamException();

                        Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                        position = length - usedBytes;

                        if (position + state.CharsNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, position + state.CharsNeeded);

                        while (position < buffer.Length)
                        {
#if NETSTANDARD2_0
                            read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                            read = await stream.ReadAsync(buffer.AsMemory(position));
#endif
                            if (read == 0)
                            {
                                state.IsFinalBlock = true;
                                break;
                            }
                            position += read;
                            length = position;
                        }

                        if (position < state.CharsNeeded)
                            throw new EndOfStreamException();

                        state.CharsNeeded = 0;
                    }
                }

                return (T?)state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }
        public static async Task<object?> DeserializeAsync(Type type, Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
                var isFinalBlock = false;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(position));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    position += read;
                    length = position;
                }

                if (position == 0)
                {
                    if (typeDetail.CoreType == CoreType.String)
                        return (object)String.Empty;
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,

                    IsFinalBlock = isFinalBlock,
                    CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value, Graph = graph }
                };

                for (; ; )
                {
                    var usedBytes = ReadConvertBytes(buffer.AsSpan().Slice(0, length), ref state, ref decodeBuffer, ref decodeBufferPosition);

                    if (state.Ended)
                        break;

                    if (state.CharsNeeded > 0)
                    {
                        if (state.IsFinalBlock)
                            throw new EndOfStreamException();

                        Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                        position = length - usedBytes;

                        if (position + state.CharsNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, position + state.CharsNeeded);

                        while (position < buffer.Length)
                        {
#if NETSTANDARD2_0
                            read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                            read = await stream.ReadAsync(buffer.AsMemory(position));
#endif
                            if (read == 0)
                            {
                                state.IsFinalBlock = true;
                                break;
                            }
                            position += read;
                            length = position;
                        }

                        if (position < state.CharsNeeded)
                            throw new EndOfStreamException();

                        state.CharsNeeded = 0;
                    }
                }

                return state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadConvertBytes(ReadOnlySpan<byte> buffer, ref ReadState state, ref char[] decodeBuffer, ref int decodeBufferPosition)
        {
            var bufferCharOwner = BufferArrayPool<char>.Rent(buffer.Length);
            try
            {
#if NET5_0_OR_GREATER
                Span<char> chars = bufferCharOwner.AsSpan().Slice(0, buffer.Length);
                var length = encoding.GetChars(buffer, chars);
                var charPosition = Read(chars.Slice(0, length), ref state, ref decodeBuffer, ref decodeBufferPosition);
                if (!state.Ended && length != buffer.Length)
                    return encoding.GetByteCount(chars.Slice(0, charPosition));
                else
                    return charPosition;
#else
                var chars = new char[buffer.Length];
                var length = encoding.GetChars(buffer.ToArray(), 0, buffer.Length, chars, 0);
                var charPosition = Read(chars.AsSpan().Slice(0, length), ref state, ref decodeBuffer, ref decodeBufferPosition);
                if (length != buffer.Length)
                    return encoding.GetByteCount(chars, 0, charPosition);
                else
                    return charPosition;
#endif
            }
            finally
            {
                Array.Clear(bufferCharOwner, 0, bufferCharOwner.Length);
                BufferArrayPool<char>.Return(bufferCharOwner);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<char> chars, ref ReadState state, ref char[] decodeBuffer, ref int decodeBufferPosition)
        {
            var decodeBufferWriter = new CharWriter(decodeBuffer, true, decodeBufferPosition);

            var reader = new CharReader(chars);

            for (; ; )
            {
                switch (state.CurrentFrame.FrameType)
                {
                    case ReadFrameType.Value: ReadValue(ref reader, ref state); break;

                    case ReadFrameType.StringToType: ReadStringToType(ref state); break;
                    case ReadFrameType.String: ReadString(ref reader, ref state, ref decodeBufferWriter); break;

                    case ReadFrameType.Object: ReadObject(ref reader, ref state); break;
                    case ReadFrameType.Dictionary: ReadDictionary(ref reader, ref state); break;

                    case ReadFrameType.Array: ReadArray(ref reader, ref state); break;
                    case ReadFrameType.ArrayNameless: ReadArrayNameless(ref reader, ref state); break;

                    case ReadFrameType.LiteralNumber: ReadLiteralNumber(ref reader, ref state, ref decodeBufferWriter); break;
                }
                if (state.Ended)
                {
                    decodeBuffer = decodeBufferWriter.BufferOwner!;
                    return -1;
                }
                if (state.CharsNeeded > 0)
                {
                    decodeBuffer = decodeBufferWriter.BufferOwner!;
                    decodeBufferPosition = decodeBufferWriter.Length;
                    return reader.Position;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadValue(ref CharReader reader, ref ReadState state)
        {
            if (state.CurrentFrame.State == 0)
            {
                if (!reader.TryReadSkipWhiteSpace(out var c))
                {
                    state.CharsNeeded = 1;
                    return;
                }
                state.CurrentFrame.State = 1;
                state.CurrentFrame.FirstLiteralChar = c;
            }

            var typeDetail = state.CurrentFrame.TypeDetail;
            var graph = state.CurrentFrame.Graph;

            if (typeDetail != null && typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = TypeAnalyzer.GetTypeDetail(emptyImplementationType);
                state.CurrentFrame.TypeDetail = typeDetail;
            }

            switch (state.CurrentFrame.FirstLiteralChar)
            {
                case '"':
                    state.CurrentFrame.State = 0;
                    state.CurrentFrame.FrameType = ReadFrameType.StringToType;
                    state.PushFrame(new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.String, Graph = graph });
                    return;

                case '{':
                    state.CurrentFrame.State = 0;
                    if (typeDetail != null && typeDetail.SpecialType == SpecialType.Dictionary)
                        state.CurrentFrame.FrameType = ReadFrameType.Dictionary;
                    else
                        state.CurrentFrame.FrameType = ReadFrameType.Object;
                    return;

                case '[':
                    state.CurrentFrame.State = 0;
                    if (!state.Nameless || (typeDetail != null && typeDetail.IsIEnumerableGeneric))
                        state.CurrentFrame.FrameType = ReadFrameType.Array;
                    else
                        state.CurrentFrame.FrameType = ReadFrameType.ArrayNameless;
                    return;

                case 'n':
                    if (!reader.TryReadSpan(out var s, 3))
                    {
                        state.CharsNeeded = 3;
                        return;
                    }
                    state.CurrentFrame.State = 0;
                    if (s[0] != 'u' || s[1] != 'l' || s[2] != 'l')
                        throw reader.CreateException("Expected number/true/false/null");
                    if (typeDetail != null && typeDetail.CoreType.HasValue)
                        state.CurrentFrame.ResultObject = ConvertNullToType(typeDetail.CoreType.Value);
                    state.EndFrame();
                    return;

                case 't':
                    if (!reader.TryReadSpan(out s, 3))
                    {
                        state.CharsNeeded = 3;
                        return;
                    }
                    state.CurrentFrame.State = 0;
                    if (s[0] != 'r' || s[1] != 'u' || s[2] != 'e')
                        throw reader.CreateException("Expected number/true/false/null");
                    if (typeDetail != null && typeDetail.CoreType.HasValue)
                        state.CurrentFrame.ResultObject = ConvertTrueToType(typeDetail.CoreType.Value);
                    state.EndFrame();
                    return;

                case 'f':
                    if (!reader.TryReadSpan(out s, 4))
                    {
                        state.CharsNeeded = 4;
                        return;
                    }
                    state.CurrentFrame.State = 0;
                    if (s[0] != 'a' || s[1] != 'l' || s[2] != 's' || s[3] != 'e')
                        throw reader.CreateException("Expected number/true/false/null");
                    if (typeDetail != null && typeDetail.CoreType.HasValue)
                        state.CurrentFrame.ResultObject = ConvertFalseToType(typeDetail.CoreType.Value);
                    state.EndFrame();
                    return;

                default:
                    state.CurrentFrame.State = 0;
                    state.CurrentFrame.FrameType = ReadFrameType.LiteralNumber;
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadObject(ref CharReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            if (state.CurrentFrame.State == 0)
            {
                state.CurrentFrame.ResultObject = typeDetail != null && typeDetail.HasCreatorBoxed ? typeDetail.CreatorBoxed() : null;
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //property name or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '"':
                                state.CurrentFrame.State = 2;
                                state.PushFrame(new ReadFrame() { FrameType = ReadFrameType.String });
                                return;
                            case '}':
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }

                    case 2: //property seperator
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        if (c != ':')
                            throw reader.CreateException("Unexpected character");

                        var propertyName = state.LastFrameResultString;
                        if (String.IsNullOrWhiteSpace(propertyName))
                            throw reader.CreateException("Unexpected character");

                        Graph? propertyGraph = null;
                        if (typeDetail != null && TryGetMember(typeDetail, propertyName, out var memberDetail))
                        {
                            state.CurrentFrame.ObjectProperty = memberDetail;
                            propertyGraph = state.CurrentFrame.Graph?.GetChildGraph(memberDetail.Name)!;
                        }
                        else
                        {
                            state.CurrentFrame.ObjectProperty = null;
                        }

                        state.CurrentFrame.State = 3;
                        state.PushFrame(new ReadFrame() { TypeDetail = state.CurrentFrame.ObjectProperty?.TypeDetail, FrameType = ReadFrameType.Value, Graph = propertyGraph });
                        return;

                    case 3: //property value
                        if (state.CurrentFrame.ObjectProperty != null && state.CurrentFrame.ResultObject != null && state.LastFrameResultObject != null && state.CurrentFrame.ObjectProperty.HasSetterBoxed)
                        {
                            //special case nullable enum
                            if (state.CurrentFrame.ObjectProperty.TypeDetail.IsNullable && state.CurrentFrame.ObjectProperty.TypeDetail.InnerTypeDetails[0].EnumUnderlyingType.HasValue)
                                state.LastFrameResultObject = Enum.ToObject(state.CurrentFrame.ObjectProperty.TypeDetail.InnerTypeDetails[0].Type, state.LastFrameResultObject);

                            if (state.CurrentFrame.Graph != null)
                            {
                                if (state.CurrentFrame.ObjectProperty.TypeDetail.IsGraphLocalProperty)
                                {
                                    if (state.CurrentFrame.Graph.HasLocalProperty(state.CurrentFrame.ObjectProperty.Name))
                                        state.CurrentFrame.ObjectProperty.SetterBoxed(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                                }
                                else
                                {
                                    if (state.CurrentFrame.Graph.HasChild(state.CurrentFrame.ObjectProperty.Name))
                                        state.CurrentFrame.ObjectProperty.SetterBoxed(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                                }
                            }
                            else
                            {
                                state.CurrentFrame.ObjectProperty.SetterBoxed(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                            }
                        }

                        state.CurrentFrame.State = 4;
                        break;

                    case 4: //next property or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case ',':
                                state.CurrentFrame.State = 1;
                                break;
                            case '}':
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadDictionary(ref CharReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail!;

            if (state.CurrentFrame.State == 0)
            {
                if (typeDetail.Type.IsInterface)
                {
                    typeDetail = TypeAnalyzer.GetGenericTypeDetail(dictionaryType, (Type[])typeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                }

                state.CurrentFrame.ResultObject = typeDetail.CreatorBoxed();
                state.CurrentFrame.AddMethod = typeDetail.GetMethod("Add");
                state.CurrentFrame.AddMethodArgs = new object[2];
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //key or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '"':
                                reader.BackOne();
                                state.CurrentFrame.State = 2;
                                state.PushFrame(new ReadFrame() { TypeDetail = typeDetail.InnerTypeDetails[0], FrameType = ReadFrameType.Value });
                                return;
                            case '}':
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }

                    case 2: //keyvalue seperator
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        if (c != ':')
                            throw reader.CreateException("Unexpected character");

                        state.CurrentFrame.DictionaryKey = state.LastFrameResultObject;
                        state.CurrentFrame.State = 3;
                        state.PushFrame(new ReadFrame() { TypeDetail = typeDetail.InnerTypeDetails[1], FrameType = ReadFrameType.Value, });
                        return;

                    case 3: //property value
                        state.CurrentFrame.AddMethodArgs![0] = state.CurrentFrame.DictionaryKey!;
                        state.CurrentFrame.AddMethodArgs![1] = state.LastFrameResultObject;
                        _ = state.CurrentFrame.AddMethod!.Caller(state.CurrentFrame.ResultObject, state.CurrentFrame.AddMethodArgs);

                        state.CurrentFrame.State = 4;
                        break;

                    case 4: //next key or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case ',':
                                state.CurrentFrame.State = 1;
                                break;
                            case '}':
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadArray(ref CharReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var graph = state.CurrentFrame.Graph;

            if (state.CurrentFrame.State == 0)
            {
                if (typeDetail != null && typeDetail.IsIEnumerableGeneric)
                {
                    state.CurrentFrame.ArrayElementType = typeDetail.IEnumerableGenericInnerTypeDetail;
                    if (typeDetail.Type.IsArray)
                    {
                        var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, state.CurrentFrame.ArrayElementType.Type));
                        state.CurrentFrame.ResultObject = genericListType.CreatorBoxed();
                        state.CurrentFrame.AddMethod = genericListType.GetMethod("Add");
                        state.CurrentFrame.AddMethodArgs = new object[1];
                    }
                    else if (typeDetail.IsIList && typeDetail.Type.IsInterface)
                    {
                        var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, state.CurrentFrame.ArrayElementType.Type));
                        state.CurrentFrame.ResultObject = genericListType.CreatorBoxed();
                        state.CurrentFrame.AddMethod = genericListType.GetMethod("Add");
                        state.CurrentFrame.AddMethodArgs = new object[1];
                    }
                    else if (typeDetail.IsIList && !typeDetail.Type.IsInterface)
                    {
                        state.CurrentFrame.ResultObject = typeDetail.CreatorBoxed();
                        state.CurrentFrame.AddMethod = typeDetail.GetMethod("Add");
                        state.CurrentFrame.AddMethodArgs = new object[1];
                    }
                    else if (typeDetail.IsISet && typeDetail.Type.IsInterface)
                    {
                        var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericHashSetType, state.CurrentFrame.ArrayElementType.Type));
                        state.CurrentFrame.ResultObject = genericListType.CreatorBoxed();
                        state.CurrentFrame.AddMethod = genericListType.GetMethod("Add");
                        state.CurrentFrame.AddMethodArgs = new object[1];
                    }
                    else if (typeDetail.IsISet && !typeDetail.Type.IsInterface)
                    {
                        state.CurrentFrame.ResultObject = typeDetail.CreatorBoxed();
                        state.CurrentFrame.AddMethod = typeDetail.GetMethod("Add");
                        state.CurrentFrame.AddMethodArgs = new object[1];
                    }
                    else
                    {
                        var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, state.CurrentFrame.ArrayElementType.Type));
                        state.CurrentFrame.ResultObject = genericListType.CreatorBoxed();
                        state.CurrentFrame.AddMethod = genericListType.GetMethod("Add");
                        state.CurrentFrame.AddMethodArgs = new object[1];
                    }
                }
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //array value or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }

                        if (c == ']')
                        {
                            if (state.CurrentFrame.ResultObject != null)
                            {
                                if (typeDetail != null && typeDetail.Type.IsArray && state.CurrentFrame.ArrayElementType != null)
                                {
                                    var list = (IList)state.CurrentFrame.ResultObject;
                                    var array = Array.CreateInstance(state.CurrentFrame.ArrayElementType.Type, list.Count);
                                    for (var i = 0; i < list.Count; i++)
                                        array.SetValue(list[i], i);
                                    state.CurrentFrame.ResultObject = array;
                                }
                            }
                            state.EndFrame();
                            return;
                        }

                        reader.BackOne();

                        state.CurrentFrame.State = 2;
                        state.PushFrame(new ReadFrame() { TypeDetail = state.CurrentFrame.ArrayElementType, FrameType = ReadFrameType.Value, Graph = graph });
                        return;

                    case 2: //array value
                        if (state.CurrentFrame.ResultObject != null)
                        {
                            //special case nullable enum
                            if (state.CurrentFrame.ArrayElementType!.IsNullable && state.CurrentFrame.ArrayElementType.InnerTypeDetails[0].EnumUnderlyingType.HasValue && state.LastFrameResultObject != null)
                                state.LastFrameResultObject = Enum.ToObject(state.CurrentFrame.ArrayElementType.InnerTypeDetails[0].Type, state.LastFrameResultObject);

                            state.CurrentFrame.AddMethodArgs![0] = state.LastFrameResultObject;
                            _ = state.CurrentFrame.AddMethod!.Caller(state.CurrentFrame.ResultObject, state.CurrentFrame.AddMethodArgs);
                        }

                        state.CurrentFrame.State = 3;
                        break;

                    case 3: //next array value or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case ',':
                                state.CurrentFrame.State = 1;
                                break;
                            case ']':
                                if (state.CurrentFrame.ResultObject != null)
                                {
                                    if (typeDetail != null && typeDetail.Type.IsArray && state.CurrentFrame.ArrayElementType != null)
                                    {
                                        var list = (IList)state.CurrentFrame.ResultObject;
                                        var array = Array.CreateInstance(state.CurrentFrame.ArrayElementType.Type, list.Count);
                                        for (var i = 0; i < list.Count; i++)
                                            array.SetValue(list[i], i);
                                        state.CurrentFrame.ResultObject = array;
                                    }
                                }
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadArrayNameless(ref CharReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            var graph = state.CurrentFrame.Graph;

            if (state.CurrentFrame.State == 0)
            {
                state.CurrentFrame.ResultObject = typeDetail != null && typeDetail.HasCreatorBoxed ? typeDetail.CreatorBoxed() : null;
                state.CurrentFrame.State = 1;
            }

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //array value or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }

                        if (c == ']')
                        {
                            state.EndFrame();
                            return;
                        }

                        reader.BackOne();

                        var memberDetail = typeDetail != null && state.CurrentFrame.PropertyIndexForNameless < typeDetail.SerializableMemberDetails.Count
                          ? typeDetail.SerializableMemberDetails[state.CurrentFrame.PropertyIndexForNameless]
                          : null;
                        state.CurrentFrame.State = 2;
                        state.PushFrame(new ReadFrame() { TypeDetail = memberDetail?.TypeDetail, FrameType = ReadFrameType.Value, Graph = graph });
                        return;

                    case 2: //array value

                        if (state.CurrentFrame.ResultObject != null && state.LastFrameResultObject != null)
                        {
                            memberDetail = typeDetail != null && state.CurrentFrame.PropertyIndexForNameless < typeDetail.SerializableMemberDetails.Count
                                ? typeDetail.SerializableMemberDetails[state.CurrentFrame.PropertyIndexForNameless]
                                : null;
                            if (memberDetail != null && memberDetail.HasSetterBoxed)
                            {
                                var propertyGraph = state.CurrentFrame.Graph?.GetChildGraph(memberDetail.Name);
                                if (memberDetail.TypeDetail.SpecialType.HasValue && memberDetail.TypeDetail.SpecialType == SpecialType.Dictionary)
                                {
                                    var innerItemEnumerable = TypeAnalyzer.GetGenericType(enumerableType, memberDetail.TypeDetail.IEnumerableGenericInnerType);
                                    object dictionary;
                                    if (memberDetail.TypeDetail.Type.IsInterface)
                                    {
                                        var dictionaryGenericType = TypeAnalyzer.GetGenericType(dictionaryType, (Type[])memberDetail.TypeDetail.IEnumerableGenericInnerTypeDetail.InnerTypes);
                                        dictionary = Instantiator.Create(dictionaryGenericType, new Type[] { innerItemEnumerable }, state.LastFrameResultObject);
                                    }
                                    else
                                    {
                                        dictionary = Instantiator.Create(typeDetail!.Type, new Type[] { innerItemEnumerable }, state.LastFrameResultObject);
                                    }

                                    memberDetail.SetterBoxed(state.CurrentFrame.ResultObject, dictionary);
                                }
                                else
                                {
                                    //special case nullable enum
                                    if (memberDetail.TypeDetail.IsNullable && memberDetail.TypeDetail.InnerTypeDetails[0].EnumUnderlyingType.HasValue)
                                        state.LastFrameResultObject = Enum.ToObject(memberDetail.TypeDetail.InnerTypeDetails[0].Type, state.LastFrameResultObject);

                                    if (state.CurrentFrame.Graph != null)
                                    {
                                        if (memberDetail.TypeDetail.IsGraphLocalProperty)
                                        {
                                            if (state.CurrentFrame.Graph.HasLocalProperty(memberDetail.Name))
                                                memberDetail.SetterBoxed(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                                        }
                                        else
                                        {
                                            if (propertyGraph != null)
                                                memberDetail.SetterBoxed(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                                        }
                                    }
                                    else
                                    {
                                        memberDetail.SetterBoxed(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                                    }
                                }
                            }
                        }
                        state.CurrentFrame.PropertyIndexForNameless++;
                        state.CurrentFrame.State = 3;
                        break;

                    case 3: //next array value or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case ',':
                                state.CurrentFrame.State = 1;
                                break;
                            case ']':
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumber(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            var coreType = typeDetail == null ? null : typeDetail.CoreType ?? typeDetail.EnumUnderlyingType;
            switch (coreType)
            {
                case CoreType.String:
                    ReadLiteralNumberAsString(ref reader, ref state, ref decodeBuffer);
                    break;
                case CoreType.Byte:
                case CoreType.ByteNullable:
                    ReadLiteralNumberAsUInt64(ref reader, ref state);
                    break;
                case CoreType.SByte:
                case CoreType.SByteNullable:
                    ReadLiteralNumberAsInt64(ref reader, ref state);
                    break;
                case CoreType.Int16:
                case CoreType.Int16Nullable:
                    ReadLiteralNumberAsInt64(ref reader, ref state);
                    break;
                case CoreType.UInt16:
                case CoreType.UInt16Nullable:
                    ReadLiteralNumberAsUInt64(ref reader, ref state);
                    break;
                case CoreType.Int32:
                case CoreType.Int32Nullable:
                    ReadLiteralNumberAsInt64(ref reader, ref state);
                    break;
                case CoreType.UInt32:
                case CoreType.UInt32Nullable:
                    ReadLiteralNumberAsUInt64(ref reader, ref state);
                    break;
                case CoreType.Int64:
                case CoreType.Int64Nullable:
                    ReadLiteralNumberAsInt64(ref reader, ref state);
                    break;
                case CoreType.UInt64:
                case CoreType.UInt64Nullable:
                    ReadLiteralNumberAsUInt64(ref reader, ref state);
                    break;
                case CoreType.Single:
                case CoreType.SingleNullable:
                    ReadLiteralNumberAsDouble(ref reader, ref state);
                    break;
                case CoreType.Double:
                case CoreType.DoubleNullable:
                    ReadLiteralNumberAsDouble(ref reader, ref state);
                    break;
                case CoreType.Decimal:
                case CoreType.DecimalNullable:
                    ReadLiteralNumberAsDecimal(ref reader, ref state);
                    break;
                default:
                    ReadLiteralNumberAsEmpty(ref reader, ref state);
                    break;
            }

            unchecked
            {
                switch (coreType)
                {
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        state.LastFrameResultObject = (byte)state.LiteralNumberUInt64;
                        break;
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        state.LastFrameResultObject = (sbyte)state.LiteralNumberInt64;
                        break;
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        state.LastFrameResultObject = (short)state.LiteralNumberInt64;
                        break;
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        state.LastFrameResultObject = (ushort)state.LiteralNumberUInt64;
                        break;
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        state.LastFrameResultObject = (int)state.LiteralNumberInt64;
                        break;
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        state.LastFrameResultObject = (uint)state.LiteralNumberUInt64;
                        break;
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        state.LastFrameResultObject = state.LiteralNumberInt64;
                        break;
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        state.LastFrameResultObject = state.LiteralNumberUInt64;
                        break;
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        state.LastFrameResultObject = (float)state.LiteralNumberDouble;
                        break;
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        state.LastFrameResultObject = state.LiteralNumberDouble;
                        break;
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        state.LastFrameResultObject = state.LiteralNumberDecimal;
                        break;
                }
            }
        }

        //private static JsonObject ReadToJsonObject(char c, ref CharReader reader, Graph graph)
        //{
        //    switch (c)
        //    {
        //        case '"':
        //            var value = ReadString(ref reader, ref decodeBuffer);
        //            return new JsonObject(value, false);
        //        case '{':
        //            return FromJsonObjectToJsonObject(ref reader, ref decodeBuffer, graph);
        //        case '[':
        //            return FromJsonArrayToJsonObject(ref reader, ref decodeBuffer, graph);
        //        default:
        //            return FromLiteralToJsonObject(c, ref reader);
        //    }
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static JsonObject ReadObjectToJsonObject(ref CharReader reader, Graph graph)
        //{
        //    var properties = new Dictionary<string, JsonObject>();
        //    var canExpectComma = false;
        //    while (reader.TryReadSkipWhiteSpace(out var c))
        //    {
        //        switch (c)
        //        {
        //            case '"':
        //                if (canExpectComma)
        //                    throw reader.CreateException("Unexpected character");
        //                var propertyName = ReadString(ref reader, ref decodeBuffer);

        //                ReadPropertySeperator(ref reader);

        //                if (!reader.TryReadSkipWhiteSpace(out c))
        //                    throw reader.CreateException("Json ended prematurely");

        //                var propertyGraph = graph?.GetChildGraph(propertyName);
        //                var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, propertyGraph);

        //                if (graph != null)
        //                {
        //                    switch (value.JsonType)
        //                    {
        //                        case JsonObject.JsonObjectType.Literal:
        //                            if (graph.HasLocalProperty(propertyName))
        //                                properties.Add(propertyName, value);
        //                            break;
        //                        case JsonObject.JsonObjectType.String:
        //                            if (graph.HasLocalProperty(propertyName))
        //                                properties.Add(propertyName, value);
        //                            break;
        //                        case JsonObject.JsonObjectType.Array:
        //                            if (graph.HasLocalProperty(propertyName))
        //                                properties.Add(propertyName, value);
        //                            break;
        //                        case JsonObject.JsonObjectType.Object:
        //                            if (graph.HasChildGraph(propertyName))
        //                                properties.Add(propertyName, value);
        //                            break;
        //                    }
        //                }
        //                else
        //                {
        //                    properties.Add(propertyName, value);
        //                }

        //                canExpectComma = true;
        //                break;
        //            case ',':
        //                if (canExpectComma)
        //                    canExpectComma = false;
        //                else
        //                    throw reader.CreateException("Unexpected character");
        //                break;
        //            case '}':
        //                return new JsonObject(properties);
        //            default:
        //                throw reader.CreateException("Unexpected character");
        //        }
        //    }
        //    throw reader.CreateException("Json ended prematurely");
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static JsonObject ReadArrayToJsonObject(ref CharReader reader, Graph graph)
        //{
        //    var arrayList = new List<JsonObject>();

        //    var canExpectComma = false;
        //    while (reader.TryReadSkipWhiteSpace(out var c))
        //    {
        //        switch (c)
        //        {
        //            case ']':
        //                return new JsonObject(arrayList.ToArray());
        //            case ',':
        //                if (canExpectComma)
        //                    canExpectComma = false;
        //                else
        //                    throw reader.CreateException("Unexpected character");
        //                break;
        //            default:
        //                if (canExpectComma)
        //                    throw reader.CreateException("Unexpected character");
        //                var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, graph);
        //                if (arrayList != null)
        //                    arrayList.Add(value);
        //                canExpectComma = true;
        //                break;
        //        }
        //    }
        //    throw reader.CreateException("Json ended prematurely");
        //}
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static JsonObject ReadLiteralToJsonObject(char c, ref CharReader reader)
        //{
        //    switch (c)
        //    {
        //        case 'n':
        //            {
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'u')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'l')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'l')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                return new JsonObject(null, true);
        //            }
        //        case 't':
        //            {
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'r')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'u')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'e')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                return new JsonObject("true", true);
        //            }
        //        case 'f':
        //            {
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'a')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'l')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 's')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                if (!reader.TryRead(out c))
        //                    throw reader.CreateException("Json ended prematurely");
        //                if (c != 'e')
        //                    throw reader.CreateException("Expected number/true/false/null");
        //                return new JsonObject("false", true);
        //            }
        //        default:
        //            {
        //                var value = FromStringLiteralNumberAsString(c, ref reader);
        //                return new JsonObject(value, true);
        //            }
        //    }
        //    throw reader.CreateException("Json ended prematurely");
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadString(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            char c;
            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 0: //reading segment
                        if (!reader.TryReadSpanUntil(out var s, '\"', '\\'))
                        {
                            state.CurrentFrame.State = 0;
                            state.CharsNeeded = 1;
                            return;
                        }
                        decodeBuffer.Write(s.Slice(0, s.Length - 1));
                        c = s[s.Length - 1];
                        switch (c)
                        {
                            case '\"':
                                state.CurrentFrame.ResultString = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            case '\\':
                                state.CurrentFrame.State = 1;
                                break;
                        }
                        break;
                    case 1: //reading escape

                        if (!reader.TryRead(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }

                        switch (c)
                        {
                            case 'b':
                                decodeBuffer.Write('\b');
                                state.CurrentFrame.State = 0;
                                break;
                            case 't':
                                decodeBuffer.Write('\t');
                                state.CurrentFrame.State = 0;
                                break;
                            case 'n':
                                decodeBuffer.Write('\n');
                                state.CurrentFrame.State = 0;
                                break;
                            case 'f':
                                decodeBuffer.Write('\f');
                                state.CurrentFrame.State = 0;
                                break;
                            case 'r':
                                decodeBuffer.Write('\r');
                                state.CurrentFrame.State = 0;
                                break;
                            case 'u':
                                state.CurrentFrame.State = 2;
                                break;
                            default:
                                decodeBuffer.Write(c);
                                state.CurrentFrame.State = 0;
                                break;
                        }

                        break;
                    case 2: //reading escape unicode
                        if (!reader.TryReadSpan(out var unicodeSpan, 4))
                        {
                            state.CharsNeeded = 4;
                            return;
                        }
                        if (!lowUnicodeHexToChar.TryGetValue(unicodeSpan.ToString(), out var unicodeChar))
                            throw reader.CreateException("Incomplete escape sequence");
                        decodeBuffer.Write(unicodeChar);
                        state.CurrentFrame.State = 0;
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadStringToType(ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            state.CurrentFrame.ResultObject = ConvertStringToType(state.LastFrameResultString, typeDetail);
            state.EndFrame();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsString(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 0: //first number
                        switch (state.CurrentFrame.FirstLiteralChar)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case '-': break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        decodeBuffer.Write(state.CurrentFrame.FirstLiteralChar);
                        state.CurrentFrame.State = 1;
                        break;

                    case 1: //next number
                        if (!reader.TryRead(out var c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case '.':
                                state.CurrentFrame.State = 2;
                                break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 3;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                        }
                        decodeBuffer.Write(c);
                        break;

                    case 2: //decimal
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 3;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                        }
                        decodeBuffer.Write(c);
                        break;

                    case 3: //first exponent
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case '+': break;
                            case '-': break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        decodeBuffer.Write(c);
                        state.CurrentFrame.State = 4;
                        break;

                    case 4: //exponent continue
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            return;
                        }

                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.CurrentFrame.ResultObject = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                        }
                        decodeBuffer.Write(c);
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsEmpty(ref CharReader reader, ref ReadState state)
        {
            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 0: //first number
                        switch (state.CurrentFrame.FirstLiteralChar)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case '-': break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 1;
                        break;

                    case 1: //next number
                        if (!reader.TryRead(out var c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case '.':
                                state.CurrentFrame.State = 2;
                                break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 3;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 2: //decimal
                        if (!reader.TryRead(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 3;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 3: //first exponent
                        if (!reader.TryRead(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case '+': break;
                            case '-': break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 4;
                        break;

                    case 4: //exponent continue
                        if (!reader.TryRead(out c))
                        {
                            state.CharsNeeded = 1;
                            return;
                        }

                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.EndFrame();
                                return;
                        }
                        break;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsInt64(ref CharReader reader, ref ReadState state)
        {
            if (state.CurrentFrame.State == 0)
            {
                state.LiteralNumberInt64 = 0;
                state.LiteralNumberWorkingDouble = 0;
                state.LiteralNumberIsNegative = false;
                state.LiteralNumberWorkingIsNegative = false;
                state.CurrentFrame.State = 1;
            }
            long number = state.LiteralNumberInt64;
            double workingNumber = state.LiteralNumberWorkingDouble;

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //first number
                        switch (state.CurrentFrame.FirstLiteralChar)
                        {
                            case '0': number = 0; break;
                            case '1': number = 1; break;
                            case '2': number = 2; break;
                            case '3': number = 3; break;
                            case '4': number = 4; break;
                            case '5': number = 5; break;
                            case '6': number = 6; break;
                            case '7': number = 7; break;
                            case '8': number = 8; break;
                            case '9': number = 9; break;
                            case '-':
                                number = 0;
                                state.LiteralNumberIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 2;
                        break;

                    case 2: //next number
                        if (!reader.TryRead(out var c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberInt64 = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': number *= 10; break;
                            case '1': number = number * 10 + 1; break;
                            case '2': number = number * 10 + 2; break;
                            case '3': number = number * 10 + 3; break;
                            case '4': number = number * 10 + 4; break;
                            case '5': number = number * 10 + 5; break;
                            case '6': number = number * 10 + 6; break;
                            case '7': number = number * 10 + 7; break;
                            case '8': number = number * 10 + 8; break;
                            case '9': number = number * 10 + 9; break;
                            case '.':
                                state.CurrentFrame.State = 3;
                                break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 3: //decimal
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberInt64 = number;
                            state.LiteralNumberWorkingDouble = workingNumber;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 4: //first exponent
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberInt64 = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': workingNumber = 0; break;
                            case '1': workingNumber = 1; break;
                            case '2': workingNumber = 2; break;
                            case '3': workingNumber = 3; break;
                            case '4': workingNumber = 4; break;
                            case '5': workingNumber = 5; break;
                            case '6': workingNumber = 6; break;
                            case '7': workingNumber = 7; break;
                            case '8': workingNumber = 8; break;
                            case '9': workingNumber = 9; break;
                            case '+': workingNumber = 0; break;
                            case '-':
                                workingNumber = 0;
                                state.LiteralNumberWorkingIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 5;
                        break;

                    case 5: //exponent continue
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (long)Math.Pow(10, workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberInt64 = number;
                            state.LiteralNumberWorkingDouble = workingNumber;
                            return;
                        }

                        switch (c)
                        {
                            case '0': workingNumber *= 10; break;
                            case '1': workingNumber = workingNumber * 10 + 1; break;
                            case '2': workingNumber = workingNumber * 10 + 2; break;
                            case '3': workingNumber = workingNumber * 10 + 3; break;
                            case '4': workingNumber = workingNumber * 10 + 4; break;
                            case '5': workingNumber = workingNumber * 10 + 5; break;
                            case '6': workingNumber = workingNumber * 10 + 6; break;
                            case '7': workingNumber = workingNumber * 10 + 7; break;
                            case '8': workingNumber = workingNumber * 10 + 8; break;
                            case '9': workingNumber = workingNumber * 10 + 9; break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (long)Math.Pow(10, workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (long)Math.Pow(10, workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberInt64 = number;
                                state.EndFrame();
                                return;
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsUInt64(ref CharReader reader, ref ReadState state)
        {
            if (state.CurrentFrame.State == 0)
            {
                state.LiteralNumberUInt64 = 0;
                state.LiteralNumberWorkingDouble = 0;
                state.LiteralNumberIsNegative = false;
                state.LiteralNumberWorkingIsNegative = false;
                state.CurrentFrame.State = 1;
            }
            ulong number = state.LiteralNumberUInt64;
            double workingNumber = state.LiteralNumberWorkingDouble;

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //first number
                        switch (state.CurrentFrame.FirstLiteralChar)
                        {
                            case '0': number = 0; break;
                            case '1': number = 1; break;
                            case '2': number = 2; break;
                            case '3': number = 3; break;
                            case '4': number = 4; break;
                            case '5': number = 5; break;
                            case '6': number = 6; break;
                            case '7': number = 7; break;
                            case '8': number = 8; break;
                            case '9': number = 9; break;
                            case '-':
                                number = 0;
                                state.LiteralNumberIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 2;
                        break;

                    case 2: //next number
                        if (!reader.TryRead(out var c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberUInt64 = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': number *= 10; break;
                            case '1': number = number * 10 + 1; break;
                            case '2': number = number * 10 + 2; break;
                            case '3': number = number * 10 + 3; break;
                            case '4': number = number * 10 + 4; break;
                            case '5': number = number * 10 + 5; break;
                            case '6': number = number * 10 + 6; break;
                            case '7': number = number * 10 + 7; break;
                            case '8': number = number * 10 + 8; break;
                            case '9': number = number * 10 + 9; break;
                            case '.':
                                state.CurrentFrame.State = 3;
                                break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 3: //decimal
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberUInt64 = number;
                            state.LiteralNumberWorkingDouble = workingNumber;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': break;
                            case '2': break;
                            case '3': break;
                            case '4': break;
                            case '5': break;
                            case '6': break;
                            case '7': break;
                            case '8': break;
                            case '9': break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 4: //first exponent
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberUInt64 = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': workingNumber = 0; break;
                            case '1': workingNumber = 1; break;
                            case '2': workingNumber = 2; break;
                            case '3': workingNumber = 3; break;
                            case '4': workingNumber = 4; break;
                            case '5': workingNumber = 5; break;
                            case '6': workingNumber = 6; break;
                            case '7': workingNumber = 7; break;
                            case '8': workingNumber = 8; break;
                            case '9': workingNumber = 9; break;
                            case '+': workingNumber = 0; break;
                            case '-':
                                workingNumber = 0;
                                state.LiteralNumberWorkingIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 5;
                        break;

                    case 5: //exponent continue
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberUInt64 = number;
                            state.LiteralNumberWorkingDouble = workingNumber;
                            return;
                        }

                        switch (c)
                        {
                            case '0': workingNumber *= 10; break;
                            case '1': workingNumber = workingNumber * 10 + 1; break;
                            case '2': workingNumber = workingNumber * 10 + 2; break;
                            case '3': workingNumber = workingNumber * 10 + 3; break;
                            case '4': workingNumber = workingNumber * 10 + 4; break;
                            case '5': workingNumber = workingNumber * 10 + 5; break;
                            case '6': workingNumber = workingNumber * 10 + 6; break;
                            case '7': workingNumber = workingNumber * 10 + 7; break;
                            case '8': workingNumber = workingNumber * 10 + 8; break;
                            case '9': workingNumber = workingNumber * 10 + 9; break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (ulong)Math.Pow(10, (ulong)workingNumber);
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (ulong)Math.Pow(10, (ulong)workingNumber);
                                state.LiteralNumberUInt64 = number;
                                state.EndFrame();
                                return;
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsDouble(ref CharReader reader, ref ReadState state)
        {
            if (state.CurrentFrame.State == 0)
            {
                state.LiteralNumberDouble = 0;
                state.LiteralNumberWorkingDouble = 0;
                state.LiteralNumberIsNegative = false;
                state.LiteralNumberWorkingIsNegative = false;
                state.CurrentFrame.State = 1;
            }
            double number = state.LiteralNumberDouble;
            double workingNumber = state.LiteralNumberWorkingDouble;

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //first number
                        switch (state.CurrentFrame.FirstLiteralChar)
                        {
                            case '0': number = 0; break;
                            case '1': number = 1; break;
                            case '2': number = 2; break;
                            case '3': number = 3; break;
                            case '4': number = 4; break;
                            case '5': number = 5; break;
                            case '6': number = 6; break;
                            case '7': number = 7; break;
                            case '8': number = 8; break;
                            case '9': number = 9; break;
                            case '-':
                                number = 0;
                                state.LiteralNumberIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 2;
                        break;

                    case 2: //next number
                        if (!reader.TryRead(out var c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDouble = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': number *= 10; break;
                            case '1': number = number * 10 + 1; break;
                            case '2': number = number * 10 + 2; break;
                            case '3': number = number * 10 + 3; break;
                            case '4': number = number * 10 + 4; break;
                            case '5': number = number * 10 + 5; break;
                            case '6': number = number * 10 + 6; break;
                            case '7': number = number * 10 + 7; break;
                            case '8': number = number * 10 + 8; break;
                            case '9': number = number * 10 + 9; break;
                            case '.':
                                workingNumber = 10;
                                state.CurrentFrame.State = 3;
                                break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 3: //decimal
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDouble = number;
                            state.LiteralNumberWorkingDouble = workingNumber;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': number += 1 / workingNumber; break;
                            case '2': number += 2 / workingNumber; break;
                            case '3': number += 3 / workingNumber; break;
                            case '4': number += 4 / workingNumber; break;
                            case '5': number += 5 / workingNumber; break;
                            case '6': number += 6 / workingNumber; break;
                            case '7': number += 7 / workingNumber; break;
                            case '8': number += 8 / workingNumber; break;
                            case '9': number += 9 / workingNumber; break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                        }
                        workingNumber *= 10;
                        break;

                    case 4: //first exponent
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDouble = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': workingNumber = 0; break;
                            case '1': workingNumber = 1; break;
                            case '2': workingNumber = 2; break;
                            case '3': workingNumber = 3; break;
                            case '4': workingNumber = 4; break;
                            case '5': workingNumber = 5; break;
                            case '6': workingNumber = 6; break;
                            case '7': workingNumber = 7; break;
                            case '8': workingNumber = 8; break;
                            case '9': workingNumber = 9; break;
                            case '+': workingNumber = 0; break;
                            case '-':
                                workingNumber = 0;
                                state.LiteralNumberWorkingIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 5;
                        break;

                    case 5: //exponent continue
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= Math.Pow(10, workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDouble = number;
                            state.LiteralNumberWorkingDouble = workingNumber;
                            return;
                        }

                        switch (c)
                        {
                            case '0': workingNumber *= 10; break;
                            case '1': workingNumber = workingNumber * 10 + 1; break;
                            case '2': workingNumber = workingNumber * 10 + 2; break;
                            case '3': workingNumber = workingNumber * 10 + 3; break;
                            case '4': workingNumber = workingNumber * 10 + 4; break;
                            case '5': workingNumber = workingNumber * 10 + 5; break;
                            case '6': workingNumber = workingNumber * 10 + 6; break;
                            case '7': workingNumber = workingNumber * 10 + 7; break;
                            case '8': workingNumber = workingNumber * 10 + 8; break;
                            case '9': workingNumber = workingNumber * 10 + 9; break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= Math.Pow(10, workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= Math.Pow(10, workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDouble = number;
                                state.EndFrame();
                                return;
                        }
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsDecimal(ref CharReader reader, ref ReadState state)
        {
            if (state.CurrentFrame.State == 0)
            {
                state.LiteralNumberDecimal = 0;
                state.LiteralNumberWorkingDecimal = 0;
                state.LiteralNumberIsNegative = false;
                state.LiteralNumberWorkingIsNegative = false;
                state.CurrentFrame.State = 1;
            }
            decimal number = state.LiteralNumberDecimal;
            decimal workingNumber = state.LiteralNumberWorkingDecimal;

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 1: //first number
                        switch (state.CurrentFrame.FirstLiteralChar)
                        {
                            case '0': number = 0; break;
                            case '1': number = 1; break;
                            case '2': number = 2; break;
                            case '3': number = 3; break;
                            case '4': number = 4; break;
                            case '5': number = 5; break;
                            case '6': number = 6; break;
                            case '7': number = 7; break;
                            case '8': number = 8; break;
                            case '9': number = 9; break;
                            case '-':
                                number = 0;
                                state.LiteralNumberIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 2;
                        break;

                    case 2: //next number
                        if (!reader.TryRead(out var c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDecimal = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': number *= 10; break;
                            case '1': number = number * 10 + 1; break;
                            case '2': number = number * 10 + 2; break;
                            case '3': number = number * 10 + 3; break;
                            case '4': number = number * 10 + 4; break;
                            case '5': number = number * 10 + 5; break;
                            case '6': number = number * 10 + 6; break;
                            case '7': number = number * 10 + 7; break;
                            case '8': number = number * 10 + 8; break;
                            case '9': number = number * 10 + 9; break;
                            case '.':
                                workingNumber = 10;
                                state.CurrentFrame.State = 3;
                                break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                        }
                        break;

                    case 3: //decimal
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDecimal = number;
                            state.LiteralNumberWorkingDecimal = workingNumber;
                            return;
                        }
                        switch (c)
                        {
                            case '0': break;
                            case '1': number += 1 / workingNumber; break;
                            case '2': number += 2 / workingNumber; break;
                            case '3': number += 3 / workingNumber; break;
                            case '4': number += 4 / workingNumber; break;
                            case '5': number += 5 / workingNumber; break;
                            case '6': number += 6 / workingNumber; break;
                            case '7': number += 7 / workingNumber; break;
                            case '8': number += 8 / workingNumber; break;
                            case '9': number += 9 / workingNumber; break;
                            case 'e':
                            case 'E':
                                state.CurrentFrame.State = 4;
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                        }
                        workingNumber *= 10;
                        break;

                    case 4: //first exponent
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDecimal = number;
                            return;
                        }
                        switch (c)
                        {
                            case '0': workingNumber = 0; break;
                            case '1': workingNumber = 1; break;
                            case '2': workingNumber = 2; break;
                            case '3': workingNumber = 3; break;
                            case '4': workingNumber = 4; break;
                            case '5': workingNumber = 5; break;
                            case '6': workingNumber = 6; break;
                            case '7': workingNumber = 7; break;
                            case '8': workingNumber = 8; break;
                            case '9': workingNumber = 9; break;
                            case '+': workingNumber = 0; break;
                            case '-':
                                workingNumber = 0;
                                state.LiteralNumberWorkingIsNegative = true;
                                break;
                            default: throw reader.CreateException("Unexpected character");
                        }
                        state.CurrentFrame.State = 5;
                        break;

                    case 5: //exponent continue
                        if (!reader.TryRead(out c))
                        {
                            if (state.IsFinalBlock)
                            {
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (decimal)Math.Pow(10, (double)workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            }
                            state.CharsNeeded = 1;
                            state.LiteralNumberDecimal = number;
                            state.LiteralNumberWorkingDecimal = workingNumber;
                            return;
                        }

                        switch (c)
                        {
                            case '0': workingNumber *= 10; break;
                            case '1': workingNumber = workingNumber * 10 + 1; break;
                            case '2': workingNumber = workingNumber * 10 + 2; break;
                            case '3': workingNumber = workingNumber * 10 + 3; break;
                            case '4': workingNumber = workingNumber * 10 + 4; break;
                            case '5': workingNumber = workingNumber * 10 + 5; break;
                            case '6': workingNumber = workingNumber * 10 + 6; break;
                            case '7': workingNumber = workingNumber * 10 + 7; break;
                            case '8': workingNumber = workingNumber * 10 + 8; break;
                            case '9': workingNumber = workingNumber * 10 + 9; break;
                            case ' ':
                            case '\r':
                            case '\n':
                            case '\t':
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (decimal)Math.Pow(10, (double)workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                            case ',':
                            case '}':
                            case ']':
                                reader.BackOne();
                                if (state.LiteralNumberWorkingIsNegative)
                                    workingNumber *= -1;
                                number *= (decimal)Math.Pow(10, (double)workingNumber);
                                if (state.LiteralNumberIsNegative)
                                    number *= -1;
                                state.LiteralNumberDecimal = number;
                                state.EndFrame();
                                return;
                        }
                        break;
                }
            }
        }
    }
}