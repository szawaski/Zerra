// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class JsonSerializer
    {
        public static T DeserializeStackBased<T>(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            return (T)DeserializeStackBased(typeof(T), bytes);
        }
        public static object DeserializeStackBased(Type type, byte[] bytes)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
                var state = new ReadState();
                state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value };


                Read(bytes, true, ref state, ref decodeBuffer, ref decodeBufferPosition);

                if (!state.Ended || state.BytesNeeded > 0)
                    throw new EndOfStreamException();

                return state.LastFrameResultObject;
            }
            finally
            {
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }

        public static T Deserialize<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            return (T)Deserialize(typeof(T), stream);
        }
        public static object Deserialize(Type type, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            var isFinalBlock = false;
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {

#if NETSTANDARD2_0
                var read = stream.Read(buffer, 0, buffer.Length);
#else
                var read = stream.Read(buffer.AsSpan());
#endif

                if (read == 0)
                {
                    isFinalBlock = true;
                    return null;
                }

                var length = read;

                var state = new ReadState();
                state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value };

                for (; ; )
                {
                    Read(buffer.AsSpan().Slice(0, length), isFinalBlock, ref state, ref decodeBuffer, ref decodeBufferPosition);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (isFinalBlock)
                            throw new EndOfStreamException();
                        var position = state.BufferPostion;

                        Buffer.BlockCopy(buffer, position, buffer, 0, length - position);
                        position = length - position;

                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

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

                        if (position < state.BytesNeeded)
                            throw new EndOfStreamException();

                        state.BytesNeeded = 0;
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

        public static async Task<T> DeserializeAsync<T>(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var type = typeof(T);

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            var isFinalBlock = false;
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {

#if NETSTANDARD2_0
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
#else
                var read = await stream.ReadAsync(buffer.AsMemory());
#endif

                if (read == 0)
                {
                    isFinalBlock = true;
                    return default;
                }

                var length = read;

                var state = new ReadState();
                state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value };

                for (; ; )
                {
                    Read(buffer.AsSpan().Slice(0, length), isFinalBlock, ref state, ref decodeBuffer, ref decodeBufferPosition);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (isFinalBlock)
                            throw new EndOfStreamException();
                        var position = state.BufferPostion;

                        Buffer.BlockCopy(buffer, position, buffer, 0, length - position);
                        position = length - position;

                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

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

                        if (position < state.BytesNeeded)
                            throw new EndOfStreamException();

                        state.BytesNeeded = 0;
                    }
                }

                return (T)state.LastFrameResultObject;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
                BufferArrayPool<char>.Return(decodeBuffer);
            }
        }
        public static async Task<object> DeserializeAsync(Type type, Stream stream)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var typeDetail = TypeAnalyzer.GetTypeDetail(type);

            var isFinalBlock = false;
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
            var decodeBuffer = BufferArrayPool<char>.Rent(defaultDecodeBufferSize);
            var decodeBufferPosition = 0;

            try
            {
#if NETSTANDARD2_0
                var read = await stream.ReadAsync(buffer, 0, buffer.Length);
#else
                var read = await stream.ReadAsync(buffer.AsMemory());
#endif
                if (read == 0)
                {
                    isFinalBlock = true;
                    return default;
                }

                var length = read;

                var state = new ReadState();
                state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Value };

                for (; ; )
                {
                    Read(buffer.AsSpan().Slice(0, read), isFinalBlock, ref state, ref decodeBuffer, ref decodeBufferPosition);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (isFinalBlock)
                            throw new EndOfStreamException();
                        var position = state.BufferPostion;

                        Buffer.BlockCopy(buffer, position, buffer, 0, length - position);
                        position = length - position;

                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

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

                        if (position < state.BytesNeeded)
                            throw new EndOfStreamException();

                        state.BytesNeeded = 0;
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

        private static void Read(ReadOnlySpan<byte> buffer, bool isFinalBlock, ref ReadState state, ref char[] decodeBuffer, ref int decodeBufferPosition)
        {
            Span<char> chars = new char[buffer.Length];
            _ = encoding.GetChars(buffer, chars);

            var decodeBufferWriter = new CharWriter(decodeBuffer, true, decodeBufferPosition);

            var reader = new CharReader(chars); //isFinalBlock);

            for (; ; )
            {
                switch (state.CurrentFrame.FrameType)
                {
                    case ReadFrameType.Value: ReadValue(ref reader, ref state, ref decodeBufferWriter); break;

                    case ReadFrameType.String: ReadString(ref reader, ref state, ref decodeBufferWriter); break;
                    case ReadFrameType.StringToType: ReadStringToType(ref reader, ref state); break;

                    case ReadFrameType.Object: ReadObject(ref reader, ref state, ref decodeBufferWriter); break;
                    case ReadFrameType.Array: ReadArray(ref reader, ref state, ref decodeBufferWriter); break;
                }
                if (state.Ended)
                {
                    return;
                }
                if (state.BytesNeeded > 0)
                {
                    state.BufferPostion = reader.Position;
                    decodeBufferPosition = decodeBufferWriter.Position;
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadValue(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            if (typeDetail.Type.IsInterface && !typeDetail.IsIEnumerable)
            {
                var emptyImplementationType = EmptyImplementations.GetEmptyImplementationType(typeDetail.Type);
                typeDetail = TypeAnalyzer.GetTypeDetail(emptyImplementationType);
            }

            if (!reader.TryReadSkipWhiteSpace(out var c))
            {
                state.BytesNeeded = 1;
                return;
            }

            switch (c)
            {
                case '"':
                    state.PushFrame();
                    state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.StringToType };
                    state.PushFrame();
                    state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.String };
                    return;
                case '{':
                    state.PushFrame();
                    state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.Object };
                    return;
                case '[':
                    state.PushFrame();
                    if (!state.Nameless || (typeDetail != null && typeDetail.IsIEnumerableGeneric))
                        state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.Array };
                    else
                        state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.Array };
                    return;
                default:
                    state.PushFrame();
                    state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Literal };
                    return;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadObject(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 0: //start
                        state.CurrentFrame.ResultObject = typeDetail.Creator();
                        state.CurrentFrame.State = 1;
                        break;

                    case 1: //property name or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.BytesNeeded = 1;
                            return;
                        }
                        switch (c)
                        {
                            case '"':
                                state.CurrentFrame.State = 2;
                                state.PushFrame();
                                state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.String };
                                break;
                            case '}':
                                state.EndFrame();
                                return;
                            default:
                                throw reader.CreateException("Unexpected character");
                        }
                        break;

                    case 2: //property seperator
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.BytesNeeded = 1;
                            return;
                        }
                        if (c != ':')
                            throw reader.CreateException("Unexpected character");

                        var propertyName = state.LastFrameResultString;

                        if (typeDetail.TryGetMemberCaseInsensitive(propertyName, out var memberDetail))
                            state.CurrentFrame.ObjectProperty = memberDetail;

                        state.PushFrame();
                        state.CurrentFrame = new ReadFrame() { TypeDetail = state.CurrentFrame.ObjectProperty?.TypeDetail, FrameType = ReadFrameType.Value };

                        state.CurrentFrame.State = 3;
                        break;

                    case 3: //property value
                        if (typeDetail != null && typeDetail.SpecialType == SpecialType.Dictionary)
                        {
                            //Dictionary Special Case
                            throw new NotImplementedException();
                        }
                        else
                        {
                            if (state.CurrentFrame.ObjectProperty != null)
                            {
                                state.CurrentFrame.ObjectProperty.Setter(state.CurrentFrame.ResultObject, state.LastFrameResultObject);
                            }
                        }
                        state.CurrentFrame.State = 3;
                        break;

                    case 4: //next property or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.BytesNeeded = 1;
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
        private static void ReadArray(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;

            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 0: //start
                        if (typeDetail != null && typeDetail.IsIEnumerableGeneric)
                        {
                            state.CurrentFrame.ArrayElementType = typeDetail.IEnumerableGenericInnerTypeDetails;
                            if (typeDetail.Type.IsArray)
                            {
                                var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, typeDetail.InnerTypeDetails[0].Type));
                                state.CurrentFrame.ResultObject = genericListType.Creator();
                                state.CurrentFrame.ArrayAddMethod = genericListType.GetMethod("Add");
                                state.CurrentFrame.ArrayAddMethodArgs = new object[1];
                            }
                            else if (typeDetail.IsIList && typeDetail.Type.IsInterface)
                            {
                                var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, typeDetail.InnerTypeDetails[0].Type));
                                state.CurrentFrame.ResultObject = genericListType.Creator();
                                state.CurrentFrame.ArrayAddMethod = genericListType.GetMethod("Add");
                                state.CurrentFrame.ArrayAddMethodArgs = new object[1];
                            }
                            else if (typeDetail.IsIList && !typeDetail.Type.IsInterface)
                            {
                                state.CurrentFrame.ResultObject = typeDetail.Creator();
                                state.CurrentFrame.ArrayAddMethod = typeDetail.GetMethod("Add");
                                state.CurrentFrame.ArrayAddMethodArgs = new object[1];
                            }
                            else if (typeDetail.IsISet && typeDetail.Type.IsInterface)
                            {
                                var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericHashSetType, typeDetail.InnerTypeDetails[0].Type));
                                state.CurrentFrame.ResultObject = genericListType.Creator();
                                state.CurrentFrame.ArrayAddMethod = genericListType.GetMethod("Add");
                                state.CurrentFrame.ArrayAddMethodArgs = new object[1];
                            }
                            else if (typeDetail.IsISet && !typeDetail.Type.IsInterface)
                            {
                                state.CurrentFrame.ResultObject = typeDetail.Creator();
                                state.CurrentFrame.ArrayAddMethod = typeDetail.GetMethod("Add");
                                state.CurrentFrame.ArrayAddMethodArgs = new object[1];
                            }
                            else
                            {
                                var genericListType = TypeAnalyzer.GetTypeDetail(TypeAnalyzer.GetGenericType(JsonSerializer.genericListType, typeDetail.InnerTypeDetails[0].Type));
                                state.CurrentFrame.ResultObject = genericListType.Creator();
                                state.CurrentFrame.ArrayAddMethod = genericListType.GetMethod("Add");
                                state.CurrentFrame.ArrayAddMethodArgs = new object[1];
                            }
                        }
                        state.CurrentFrame.State = 1;
                        break;

                    case 1: //array value or end
                        if (!reader.TryReadSkipWhiteSpace(out var c))
                        {
                            state.BytesNeeded = 1;
                            return;
                        }

                        switch (c)
                        {
                            case ']':
                                state.EndFrame();
                                return;
                            case '"':
                                state.PushFrame();
                                state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.StringToType };
                                state.PushFrame();
                                state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.String };
                                state.CurrentFrame.State = 2;
                                return;
                            case '{':
                                state.PushFrame();
                                state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.Object };
                                state.CurrentFrame.State = 2;
                                return;
                            case '[':
                                state.PushFrame();
                                if (!state.Nameless || (typeDetail != null && typeDetail.IsIEnumerableGeneric))
                                    state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.Array };
                                else
                                    state.CurrentFrame = new ReadFrame() { FrameType = ReadFrameType.Array };
                                state.CurrentFrame.State = 2;
                                return;
                            default:
                                state.PushFrame();
                                state.CurrentFrame = new ReadFrame() { TypeDetail = typeDetail, FrameType = ReadFrameType.Literal };
                                state.CurrentFrame.State = 2;
                                return;
                        }

                    case 2: //array value
                        if (state.CurrentFrame.ResultObject == null)
                            return;
                        if (typeDetail.Type.IsArray && state.CurrentFrame.ArrayElementType != null)
                        {
                            var list = (IList)state.CurrentFrame.ResultObject;
                            var array = Array.CreateInstance(state.CurrentFrame.ArrayElementType.Type, list.Count);
                            for (var i = 0; i < list.Count; i++)
                                array.SetValue(list[i], i);
                        }
                        state.CurrentFrame.State = 3;
                        break;

                    case 3: //next array value or end
                        if (!reader.TryReadSkipWhiteSpace(out c))
                        {
                            state.BytesNeeded = 1;
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
        private static void ReadArrayNameless(ref CharReader reader, ref ReadState readState)
        {
            var obj = typeDetail?.Creator();
            var canExpectComma = false;
            var propertyIndexForNameless = 0;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case ']':
                        return obj;
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    default:
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        if (obj != null)
                        {
                            var memberDetail = typeDetail != null && propertyIndexForNameless < typeDetail.SerializableMemberDetails.Count
                                ? typeDetail.SerializableMemberDetails[propertyIndexForNameless++]
                                : null;
                            if (memberDetail != null)
                            {
                                var propertyGraph = graph?.GetChildGraph(memberDetail.Name);
                                var value = FromJson(c, ref reader, ref decodeBuffer, memberDetail?.TypeDetail, propertyGraph, true);
                                if (memberDetail != null && memberDetail.TypeDetail.SpecialType.HasValue && memberDetail.TypeDetail.SpecialType == SpecialType.Dictionary)
                                {
                                    var dictionary = memberDetail.TypeDetail.Creator();
                                    var addMethod = memberDetail.TypeDetail.GetMethod("Add");
                                    var keyGetter = memberDetail.TypeDetail.InnerTypeDetails[0].GetMember("Key").Getter;
                                    var valueGetter = memberDetail.TypeDetail.InnerTypeDetails[0].GetMember("Value").Getter;
                                    foreach (var item in (IEnumerable)value)
                                    {
                                        var itemKey = keyGetter(item);
                                        var itemValue = valueGetter(item);
                                        _ = addMethod.Caller(dictionary, new object[] { itemKey, itemValue });
                                    }
                                    memberDetail.Setter(obj, dictionary);
                                }
                                else
                                {
                                    if (value != null)
                                    {
                                        if (graph != null)
                                        {
                                            if (memberDetail.TypeDetail.IsGraphLocalProperty)
                                            {
                                                if (graph.HasLocalProperty(memberDetail.Name))
                                                    memberDetail.Setter(obj, value);
                                            }
                                            else
                                            {
                                                if (propertyGraph != null)
                                                    memberDetail.Setter(obj, value);
                                            }
                                        }
                                        else
                                        {
                                            memberDetail.Setter(obj, value);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _ = FromJson(c, ref reader, ref decodeBuffer, null, null, true);
                            }
                        }
                        else
                        {
                            _ = FromJson(c, ref reader, ref decodeBuffer, null, null, true);
                        }
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteral(char c, ref CharReader reader, TypeDetail typeDetail)
        {
            switch (c)
            {
                case 'n':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertNullToType(typeDetail.CoreType.Value);
                        return null;
                    }
                case 't':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'r')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertTrueToType(typeDetail.CoreType.Value);
                        return null;
                    }
                case 'f':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'a')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 's')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (typeDetail != null && typeDetail.CoreType.HasValue)
                            return ConvertFalseToType(typeDetail.CoreType.Value);
                        return null;
                    }
                default:
                    {
                        if (typeDetail != null)
                        {
                            if (typeDetail.CoreType == CoreType.String)
                            {
                                var value = ReadLiteralNumberAsString(c, ref reader);
                                return value;
                            }
                            else if (typeDetail.CoreType.HasValue)
                            {
                                return ReadLiteralNumberAsType(c, typeDetail.CoreType.Value, ref reader);
                            }
                            else if (typeDetail.EnumUnderlyingType.HasValue)
                            {
                                return ReadLiteralNumberAsType(c, typeDetail.EnumUnderlyingType.Value, ref reader);
                            }
                        }
                        ReadLiteralNumberAsEmpty(c, ref reader);
                        return null;
                    }
            }
            throw reader.CreateException("Json ended prematurely");
        }

        private static JsonObject ReadToJsonObject(char c, ref CharReader reader, Graph graph)
        {
            switch (c)
            {
                case '"':
                    var value = ReadString(ref reader, ref decodeBuffer);
                    return new JsonObject(value, false);
                case '{':
                    return FromJsonObjectToJsonObject(ref reader, ref decodeBuffer, graph);
                case '[':
                    return FromJsonArrayToJsonObject(ref reader, ref decodeBuffer, graph);
                default:
                    return FromLiteralToJsonObject(c, ref reader);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject ReadObjectToJsonObject(ref CharReader reader, Graph graph)
        {
            var properties = new Dictionary<string, JsonObject>();
            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case '"':
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var propertyName = ReadString(ref reader, ref decodeBuffer);

                        ReadPropertySperator(ref reader);

                        if (!reader.TryReadSkipWhiteSpace(out c))
                            throw reader.CreateException("Json ended prematurely");

                        var propertyGraph = graph?.GetChildGraph(propertyName);
                        var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, propertyGraph);

                        if (graph != null)
                        {
                            switch (value.JsonType)
                            {
                                case JsonObject.JsonObjectType.Literal:
                                    if (graph.HasLocalProperty(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                                case JsonObject.JsonObjectType.String:
                                    if (graph.HasLocalProperty(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                                case JsonObject.JsonObjectType.Array:
                                    if (graph.HasLocalProperty(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                                case JsonObject.JsonObjectType.Object:
                                    if (graph.HasChildGraph(propertyName))
                                        properties.Add(propertyName, value);
                                    break;
                            }
                        }
                        else
                        {
                            properties.Add(propertyName, value);
                        }

                        canExpectComma = true;
                        break;
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    case '}':
                        return new JsonObject(properties);
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject ReadArrayToJsonObject(ref CharReader reader, Graph graph)
        {
            var arrayList = new List<JsonObject>();

            var canExpectComma = false;
            while (reader.TryReadSkipWhiteSpace(out var c))
            {
                switch (c)
                {
                    case ']':
                        return new JsonObject(arrayList.ToArray());
                    case ',':
                        if (canExpectComma)
                            canExpectComma = false;
                        else
                            throw reader.CreateException("Unexpected character");
                        break;
                    default:
                        if (canExpectComma)
                            throw reader.CreateException("Unexpected character");
                        var value = FromJsonToJsonObject(c, ref reader, ref decodeBuffer, graph);
                        if (arrayList != null)
                            arrayList.Add(value);
                        canExpectComma = true;
                        break;
                }
            }
            throw reader.CreateException("Json ended prematurely");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JsonObject ReadLiteralToJsonObject(char c, ref CharReader reader)
        {
            switch (c)
            {
                case 'n':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject(null, true);
                    }
                case 't':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'r')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'u')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject("true", true);
                    }
                case 'f':
                    {
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'a')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'l')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 's')
                            throw reader.CreateException("Expected number/true/false/null");
                        if (!reader.TryRead(out c))
                            throw reader.CreateException("Json ended prematurely");
                        if (c != 'e')
                            throw reader.CreateException("Expected number/true/false/null");
                        return new JsonObject("false", true);
                    }
                default:
                    {
                        var value = ReadLiteralNumberAsString(c, ref reader);
                        return new JsonObject(value, true);
                    }
            }
            throw reader.CreateException("Json ended prematurely");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadPropertySperator(ref CharReader reader)
        {
            if (!reader.TryReadSkipWhiteSpace(out var c))
                throw reader.CreateException("Json ended prematurely");
            if (c == ':')
                return;
            throw reader.CreateException("Unexpected character");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadString(ref CharReader reader, ref ReadState state, ref CharWriter decodeBuffer)
        {
            char c;
            for (; ; )
            {
                switch (state.CurrentFrame.State)
                {
                    case 0: //start
                        reader.BeginSegment(false);
                        state.CurrentFrame.State = 1;
                        break;
                    case 1: //reading segment
                        if (!reader.TryReadUntil(out c, '\"', '\\'))
                        {
                            reader.EndSegmentCopyTo(false, ref decodeBuffer);
                            state.CurrentFrame.State = 0;
                            state.BytesNeeded = 1;
                            return;
                        }

                        switch (c)
                        {
                            case '\"':
                                reader.EndSegmentCopyTo(false, ref decodeBuffer);
                                state.CurrentFrame.ResultString = decodeBuffer.ToString();
                                decodeBuffer.Clear();
                                state.EndFrame();
                                return;
                            case '\\':
                                reader.EndSegmentCopyTo(false, ref decodeBuffer);
                                state.CurrentFrame.State = 2;
                                break;
                        }
                        break;
                    case 2: //reading escape

                        if (!reader.TryRead(out c))
                        {
                            state.BytesNeeded = 1;
                            return;
                        }

                        switch (c)
                        {
                            case 'b':
                                decodeBuffer.Write('\b');
                                reader.BeginSegment(false);
                                state.CurrentFrame.State = 1;
                                break;
                            case 't':
                                decodeBuffer.Write('\t');
                                reader.BeginSegment(false);
                                state.CurrentFrame.State = 1;
                                break;
                            case 'n':
                                decodeBuffer.Write('\n');
                                reader.BeginSegment(false);
                                state.CurrentFrame.State = 1;
                                break;
                            case 'f':
                                decodeBuffer.Write('\f');
                                reader.BeginSegment(false);
                                state.CurrentFrame.State = 1;
                                break;
                            case 'r':
                                decodeBuffer.Write('\r');
                                reader.BeginSegment(false);
                                state.CurrentFrame.State = 1;
                                break;
                            case 'u':
                                state.CurrentFrame.State = 3;
                                break;
                            default:
                                decodeBuffer.Write(c);
                                reader.BeginSegment(false);
                                state.CurrentFrame.State = 1;
                                break;
                        }

                        break;
                    case 3: //reading escape unicode
                        if (!reader.TryReadString(out var unicodeString, 4))
                        {
                            state.BytesNeeded = 4;
                            return;
                        }
                        if (!lowUnicodeHexToChar.TryGetValue(unicodeString, out var unicodeChar))
                            throw reader.CreateException("Incomplete escape sequence");
                        decodeBuffer.Write(unicodeChar);
                        reader.BeginSegment(false);
                        state.CurrentFrame.State = 1;
                        break;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadStringToType(ref CharReader reader, ref ReadState state)
        {
            var typeDetail = state.CurrentFrame.TypeDetail;
            state.CurrentFrame.ResultObject = ConvertStringToType(state.LastFrameResultString, typeDetail);
            state.EndFrame();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object ReadLiteralNumberAsType(char c, CoreType coreType, ref CharReader reader)
        {
            unchecked
            {
                switch (coreType)
                {
                    case CoreType.Byte:
                    case CoreType.ByteNullable:
                        return (byte)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.SByte:
                    case CoreType.SByteNullable:
                        return (sbyte)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.Int16:
                    case CoreType.Int16Nullable:
                        return (short)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt16:
                    case CoreType.UInt16Nullable:
                        return (ushort)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Int32:
                    case CoreType.Int32Nullable:
                        return (int)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt32:
                    case CoreType.UInt32Nullable:
                        return (uint)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Int64:
                    case CoreType.Int64Nullable:
                        return (long)ReadLiteralNumberAsInt64(c, ref reader);
                    case CoreType.UInt64:
                    case CoreType.UInt64Nullable:
                        return (ulong)ReadLiteralNumberAsUInt64(c, ref reader);
                    case CoreType.Single:
                    case CoreType.SingleNullable:
                        return (float)ReadLiteralNumberAsDouble(c, ref reader);
                    case CoreType.Double:
                    case CoreType.DoubleNullable:
                        return (double)ReadLiteralNumberAsDouble(c, ref reader);
                    case CoreType.Decimal:
                    case CoreType.DecimalNullable:
                        return (decimal)ReadLiteralNumberAsDecimal(c, ref reader);
                }
            }
            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ReadLiteralNumberAsString(char c, ref CharReader reader)
        {
            reader.BeginSegment(true);

            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    //nothing
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.TryRead(out c))
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        //nothing
                        break;
                    case '.':
                        {
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        //nothing
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            switch (c)
                                            {
                                                case '0':
                                                case '1':
                                                case '2':
                                                case '3':
                                                case '4':
                                                case '5':
                                                case '6':
                                                case '7':
                                                case '8':
                                                case '9':
                                                case '-':
                                                case '+':
                                                    //nothing
                                                    break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0':
                                                    case '1':
                                                    case '2':
                                                    case '3':
                                                    case '4':
                                                    case '5':
                                                    case '6':
                                                    case '7':
                                                    case '8':
                                                    case '9':
                                                    case '+':
                                                        //nothing
                                                        break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        return reader.EndSegmentToString(false);
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        return reader.EndSegmentToString(true);
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            return reader.EndSegmentToString(true);
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return reader.EndSegmentToString(false);
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return reader.EndSegmentToString(true);
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            switch (c)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '-':
                                case '+':
                                    //nothing
                                    break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return reader.EndSegmentToString(false);
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return reader.EndSegmentToString(true);
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            return reader.EndSegmentToString(true);
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        return reader.EndSegmentToString(false);
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        return reader.EndSegmentToString(true);
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            return reader.EndSegmentToString(true);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadLiteralNumberAsInt64(char c, ref CharReader reader)
        {
            var negative = false;
            long number;

            switch (c)
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
                case '-': number = 0; negative = true; break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.TryRead(out c))
            {
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
                        {
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
                                            double exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (long)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (long)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= (long)Math.Pow(10, (double)exponent);
                                            if (negative)
                                                number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
                            double exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (long)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (long)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= (long)Math.Pow(10, (double)exponent);
                            if (negative)
                                number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative)
                            number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative)
                            number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative)
                number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadLiteralNumberAsUInt64(char c, ref CharReader reader)
        {
            var number = c switch
            {
                '0' => 0UL,
                '1' => 1UL,
                '2' => 2UL,
                '3' => 3UL,
                '4' => 4UL,
                '5' => 5UL,
                '6' => 6UL,
                '7' => 7UL,
                '8' => 8UL,
                '9' => 9UL,
                '-' => 0UL,
                _ => throw reader.CreateException("Unexpected character"),
            };
            while (reader.TryRead(out c))
            {
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
                        {
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
                                            double exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= (ulong)Math.Pow(10, (double)exponent);
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
                            double exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (ulong)Math.Pow(10, (double)exponent);
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= (ulong)Math.Pow(10, (double)exponent);
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ReadLiteralNumberAsDouble(char c, ref CharReader reader)
        {
            var negative = false;
            double number;

            switch (c)
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
                case '-': number = 0; negative = true; break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.TryRead(out c))
            {
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
                        {
                            double fraction = 10;
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0': break;
                                    case '1': number += 1 / fraction; break;
                                    case '2': number += 2 / fraction; break;
                                    case '3': number += 3 / fraction; break;
                                    case '4': number += 4 / fraction; break;
                                    case '5': number += 5 / fraction; break;
                                    case '6': number += 6 / fraction; break;
                                    case '7': number += 7 / fraction; break;
                                    case '8': number += 8 / fraction; break;
                                    case '9': number += 9 / fraction; break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
                                            double exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= Math.Pow(10, exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= Math.Pow(10, exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= Math.Pow(10, exponent);
                                            if (negative)
                                                number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                                fraction *= 10;
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
                            double exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= Math.Pow(10, exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= Math.Pow(10, exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= Math.Pow(10, exponent);
                            if (negative)
                                number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative)
                            number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative)
                            number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative)
                number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static decimal ReadLiteralNumberAsDecimal(char c, ref CharReader reader)
        {
            var negative = false;
            decimal number;

            switch (c)
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
                case '-': number = 0; negative = true; break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.TryRead(out c))
            {
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
                        {
                            decimal fraction = 10;
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0': break;
                                    case '1': number += 1 / fraction; break;
                                    case '2': number += 2 / fraction; break;
                                    case '3': number += 3 / fraction; break;
                                    case '4': number += 4 / fraction; break;
                                    case '5': number += 5 / fraction; break;
                                    case '6': number += 6 / fraction; break;
                                    case '7': number += 7 / fraction; break;
                                    case '8': number += 8 / fraction; break;
                                    case '9': number += 9 / fraction; break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            var negativeExponent = false;
                                            decimal exponent;

                                            switch (c)
                                            {
                                                case '0': exponent = 0; break;
                                                case '1': exponent = 1; break;
                                                case '2': exponent = 2; break;
                                                case '3': exponent = 3; break;
                                                case '4': exponent = 4; break;
                                                case '5': exponent = 5; break;
                                                case '6': exponent = 6; break;
                                                case '7': exponent = 7; break;
                                                case '8': exponent = 8; break;
                                                case '9': exponent = 9; break;
                                                case '+': exponent = 0; break;
                                                case '-': exponent = 0; negativeExponent = true; break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0': exponent *= 10; break;
                                                    case '1': exponent = exponent * 10 + 1; break;
                                                    case '2': exponent = exponent * 10 + 2; break;
                                                    case '3': exponent = exponent * 10 + 3; break;
                                                    case '4': exponent = exponent * 10 + 4; break;
                                                    case '5': exponent = exponent * 10 + 5; break;
                                                    case '6': exponent = exponent * 10 + 6; break;
                                                    case '7': exponent = exponent * 10 + 7; break;
                                                    case '8': exponent = exponent * 10 + 8; break;
                                                    case '9': exponent = exponent * 10 + 9; break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        if (negativeExponent)
                                                            exponent *= -1;
                                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                                        if (negative)
                                                            number *= -1;
                                                        return number;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            if (negativeExponent)
                                                exponent *= -1;
                                            number *= (decimal)Math.Pow(10, (double)exponent);
                                            if (negative)
                                                number *= -1;
                                            return number;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                                fraction *= 10;
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            var negativeExponent = false;
                            decimal exponent;

                            switch (c)
                            {
                                case '0': exponent = 0; break;
                                case '1': exponent = 1; break;
                                case '2': exponent = 2; break;
                                case '3': exponent = 3; break;
                                case '4': exponent = 4; break;
                                case '5': exponent = 5; break;
                                case '6': exponent = 6; break;
                                case '7': exponent = 7; break;
                                case '8': exponent = 8; break;
                                case '9': exponent = 9; break;
                                case '+': exponent = 0; break;
                                case '-': exponent = 0; negativeExponent = true; break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0': exponent *= 10; break;
                                    case '1': exponent = exponent * 10 + 1; break;
                                    case '2': exponent = exponent * 10 + 2; break;
                                    case '3': exponent = exponent * 10 + 3; break;
                                    case '4': exponent = exponent * 10 + 4; break;
                                    case '5': exponent = exponent * 10 + 5; break;
                                    case '6': exponent = exponent * 10 + 6; break;
                                    case '7': exponent = exponent * 10 + 7; break;
                                    case '8': exponent = exponent * 10 + 8; break;
                                    case '9': exponent = exponent * 10 + 9; break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        if (negativeExponent)
                                            exponent *= -1;
                                        number *= (decimal)Math.Pow(10, (double)exponent);
                                        if (negative)
                                            number *= -1;
                                        return number;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            if (negativeExponent)
                                exponent *= -1;
                            number *= (decimal)Math.Pow(10, (double)exponent);
                            if (negative)
                                number *= -1;
                            return number;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        if (negative)
                            number *= -1;
                        return number;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        if (negative)
                            number *= -1;
                        return number;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }

            if (negative)
                number *= -1;
            return number;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadLiteralNumberAsEmpty(char c, ref CharReader reader)
        {
            switch (c)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '-':
                    break;
                default: throw reader.CreateException("Unexpected character");
            }

            while (reader.TryRead(out c))
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        break;
                    case '.':
                        {
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case 'e':
                                    case 'E':
                                        {
                                            if (!reader.TryRead(out c))
                                                throw reader.CreateException("Json ended prematurely");

                                            switch (c)
                                            {
                                                case '0':
                                                case '1':
                                                case '2':
                                                case '3':
                                                case '4':
                                                case '5':
                                                case '6':
                                                case '7':
                                                case '8':
                                                case '9':
                                                case '+':
                                                case '-':
                                                    break;
                                                default: throw reader.CreateException("Unexpected character");
                                            }
                                            while (reader.TryRead(out c))
                                            {
                                                switch (c)
                                                {
                                                    case '0':
                                                    case '1':
                                                    case '2':
                                                    case '3':
                                                    case '4':
                                                    case '5':
                                                    case '6':
                                                    case '7':
                                                    case '8':
                                                    case '9':
                                                        break;
                                                    case ' ':
                                                    case '\r':
                                                    case '\n':
                                                    case '\t':
                                                        return;
                                                    case ',':
                                                    case '}':
                                                    case ']':
                                                        reader.BackOne();
                                                        return;
                                                    default:
                                                        throw reader.CreateException("Unexpected character");
                                                }
                                            }
                                            return;
                                        }
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                        }
                        break;
                    case 'e':
                    case 'E':
                        {
                            if (!reader.TryRead(out c))
                                throw reader.CreateException("Json ended prematurely");

                            switch (c)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '+':
                                case '-':
                                    break;
                                default: throw reader.CreateException("Unexpected character");
                            }
                            while (reader.TryRead(out c))
                            {
                                switch (c)
                                {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7':
                                    case '8':
                                    case '9':
                                        break;
                                    case ' ':
                                    case '\r':
                                    case '\n':
                                    case '\t':
                                        return;
                                    case ',':
                                    case '}':
                                    case ']':
                                        reader.BackOne();
                                        return;
                                    default:
                                        throw reader.CreateException("Unexpected character");
                                }
                            }
                            return;
                        }
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                        return;
                    case ',':
                    case '}':
                    case ']':
                        reader.BackOne();
                        return;
                    default:
                        throw reader.CreateException("Unexpected character");
                }
            }
            return;
        }
    }
}