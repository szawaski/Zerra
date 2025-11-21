// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading.Tasks;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using Zerra.Buffers;
using System.Threading;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
#if NETSTANDARD2_0
        public static (T?, Graph?) DeserializePatch<T>(string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => DeserializePatch<T>(str.AsSpan(), options, graph);
        public static (object?, Graph?) DeserializePatch(Type type, string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => DeserializePatch(str.AsSpan(), type, options, graph);
#endif

        public static (T?, Graph?) DeserializePatch<T>(ReadOnlySpan<char> chars, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (chars.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return ((T)(object)String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            T? result;

            Read(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return (result, state.Current.ReturnGraph);
        }
        public static (object?, Graph?) DeserializePatch(ReadOnlySpan<char> chars, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (chars.Length == 0)
            {
                if (type == typeof(string))
                    return (String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            object? result;

            ReadBoxed(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return (result, state.Current.ReturnGraph);
        }

        public static (T?, Graph?) DeserializePatch<T>(ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (bytes.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return ((T)(object)String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            T? result;

            Read(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return (result, state.Current.ReturnGraph);
        }
        public static (object?, Graph?) DeserializePatch(ReadOnlySpan<byte> bytes, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (bytes.Length == 0)
            {
                if (type == typeof(string))
                    return (String.Empty, null);
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true, true);

            object? result;

            ReadBoxed(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return (result, state.Current.ReturnGraph);
        }

        public static (T?, Graph?) DeserializePatch<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = stream.Read(buffer, length, buffer.Length - length);
#else
                    read = stream.Read(buffer.AsSpan(length));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (typeof(T) == typeof(string))
                        return ((T)(object)String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = stream.Read(buffer, length, buffer.Length - length);
#else
                        read = stream.Read(buffer.AsSpan(length));
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException();

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
        public static (object?, Graph?) DeserializePatch(Stream stream, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = stream.Read(buffer, length, buffer.Length - length);
#else
                    read = stream.Read(buffer.AsSpan(length));
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (type == typeof(string))
                        return (String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = stream.Read(buffer, length, buffer.Length - length);
#else
                        read = stream.Read(buffer.AsSpan(length));
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException();

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        public static async Task<(T?, Graph?)> DeserializePatchAsync<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (typeof(T) == typeof(string))
                        return ((T)(object)String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException();

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
        public static async Task<(object?, Graph?)> DeserializePatchAsync(Stream stream, Type type, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var isFinalBlock = false;
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif
                    if (read == 0)
                    {
                        isFinalBlock = true;
                        break;
                    }
                    length += read;
                }

                if (length == 0)
                {
                    if (type == typeof(string))
                        return (String.Empty, null);
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock, true);

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    if (length + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.SizeNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.SizeNeeded)
                        throw new EndOfStreamException();

                    state.SizeNeeded = 0;
                }

                return (result, state.Current.ReturnGraph);
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
    }
}
