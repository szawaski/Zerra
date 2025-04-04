// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;
using Zerra.Reflection;
using Zerra.Buffers;
using System.Threading;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
#if NETSTANDARD2_0
        public static T? Deserialize<T>(string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => Deserialize<T>(str.AsSpan(), options, graph);
        public static object? Deserialize(Type type, string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => Deserialize(type, str.AsSpan(), options, graph);
#endif

        public static T? Deserialize<T>(ReadOnlySpan<char> chars, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (chars.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)String.Empty;
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true);

            T? result;

            Read(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static object? Deserialize(Type type, ReadOnlySpan<char> chars, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (chars.Length == 0)
            {
                if (type == typeof(string))
                    return String.Empty;
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true);

            object? result;

            ReadBoxed(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (bytes.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)String.Empty;
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true);

            T? result;

            Read(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static object? Deserialize(Type type, ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (bytes.Length == 0)
            {
                if (type == typeof(string))
                    return String.Empty;
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState(options, graph, true);

            object? result;

            ReadBoxed(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static T? Deserialize<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
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
                        return (T)(object)String.Empty;
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock);

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

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
        public static object? Deserialize(Type type, Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
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
                        return String.Empty;
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock);

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

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        public static async Task<T?> DeserializeAsync<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
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
                        return (T)(object)String.Empty;
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock);

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

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
        public static async Task<object?> DeserializeAsync(Type type, Stream stream, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
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
                        return String.Empty;
                    return default;
                }

                var state = new ReadState(options, graph, isFinalBlock);

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

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read<T>(JsonConverter<object, T> converter, ReadOnlySpan<byte> buffer, ref ReadState state, out T? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock);
#if DEBUG
        again:
#endif
            var read = converter.TryRead(ref reader, ref state, out result);
            if (read)
            {
                state.SizeNeeded = 0;
            }
            else if (state.SizeNeeded == 0)
            {
#if DEBUG
                throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!read && JsonReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(JsonConverter converter, ReadOnlySpan<byte> buffer, ref ReadState state, out object? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock);
#if DEBUG
        again:
#endif
            var read = converter.TryReadBoxed(ref reader, ref state, out result);
            if (read)
            {
                state.SizeNeeded = 0;
            }
            else if (state.SizeNeeded == 0)
            {
#if DEBUG
                throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!read && JsonReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read<T>(JsonConverter<object, T> converter, ReadOnlySpan<char> buffer, ref ReadState state, out T? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock);
#if DEBUG
        again:
#endif
            var read = converter.TryRead(ref reader, ref state, out result);
            if (read)
            {
                state.SizeNeeded = 0;
            }
            else if (state.SizeNeeded == 0)
            {
#if DEBUG
                throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!read && JsonReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(JsonConverter converter, ReadOnlySpan<char> buffer, ref ReadState state, out object? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock);
#if DEBUG
        again:
#endif
            var read = converter.TryReadBoxed(ref reader, ref state, out result);
            if (read)
            {
                state.SizeNeeded = 0;
            }
            else if (state.SizeNeeded == 0)
            {
#if DEBUG
                throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!read && JsonReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static void BufferShift(byte[] buffer, int position)
        {
            fixed (byte* pBuffer = buffer)
            {
                Buffer.MemoryCopy(&pBuffer[position], pBuffer, buffer.Length, buffer.Length - position);
            }
        }
    }
}
