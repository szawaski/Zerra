// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Reflection;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        public static string? Serialize<T>(T? obj, JsonSerializerOptions? options = null)
        {
            if (obj == null)
                return null;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber
            };
            state.PushFrame();

            var result = Write(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static string? Serialize(object? obj, JsonSerializerOptions? options = null)
        {
            if (obj == null)
                return null;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber
            };
            state.PushFrame();

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }
        public static string? Serialize(object? obj, Type type, JsonSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return null;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber
            };
            state.PushFrame();

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static void Serialize<T>(Stream stream, T? obj, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber
                };
                state.PushFrame();

                for (; ; )
                {
                    var usedBytes = Write(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.CharsNeeded == 0)
                        break;

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
        public static void Serialize(Stream stream, object? obj, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber
                };
                state.PushFrame();

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.CharsNeeded == 0)
                        break;

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
        public static void Serialize(Stream stream, object? obj, Type type, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber
                };
                state.PushFrame();

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.CharsNeeded == 0)
                        break;

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

        public static async Task SerializeAsync<T>(Stream stream, T? obj, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber
                };
                state.PushFrame();

                for (; ; )
                {
                    var usedBytes = Write(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.CharsNeeded == 0)
                        break;

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
        public static async Task SerializeAsync(Stream stream, object? obj, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber
                };
                state.PushFrame();

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.CharsNeeded == 0)
                        break;

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
        public static async Task SerializeAsync(Stream stream, object? obj, Type type, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return;

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber
                };
                state.PushFrame();

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.CharsNeeded == 0)
                        break;

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
        private static string Write<T>(JsonConverter<object, T> converter, int initialSize, ref WriteState state, T value)
        {
            var writer = new CharWriter(initialSize);
            try
            {
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                    state.CharsNeeded = 0;
                var result = writer.ToString();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string WriteBoxed(JsonConverter<object> converter, int initialSize, ref WriteState state, object value)
        {
            var writer = new CharWriter(initialSize);
            try
            {
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                    state.CharsNeeded = 0;
                var result = writer.ToString();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Write<T>(JsonConverter<object, T> converter, Span<byte> buffer, ref WriteState state, T value)
        {
            var bufferCharOwner = BufferArrayPool<char>.Rent(buffer.Length);
            try
            {
#if NET5_0_OR_GREATER
                Span<char> chars = bufferCharOwner.AsSpan().Slice(0, buffer.Length);
                var length = encoding.GetChars(buffer, chars);

                var writer = new CharWriter(chars);
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                    state.CharsNeeded = 0;

                if (length != buffer.Length)
                    return encoding.GetByteCount(chars.Slice(0, writer.Length));
                else
                    return writer.Length;
#else
                var chars = new char[buffer.Length];
                var length = encoding.GetChars(buffer.ToArray(), 0, buffer.Length, chars, 0);

                var writer = new CharWriter(chars);
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                    state.CharsNeeded = 0;

                if (length != buffer.Length)
                    return encoding.GetByteCount(chars, 0, writer.Length);
                else
                    return writer.Length;
#endif
            }
            finally
            {
                Array.Clear(bufferCharOwner, 0, bufferCharOwner.Length);
                BufferArrayPool<char>.Return(bufferCharOwner);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteBoxed(JsonConverter<object> converter, Span<byte> buffer, ref WriteState state, object value)
        {
            var bufferCharOwner = BufferArrayPool<char>.Rent(buffer.Length);
            try
            {
#if NET5_0_OR_GREATER
                Span<char> chars = bufferCharOwner.AsSpan().Slice(0, buffer.Length);
                var length = encoding.GetChars(buffer, chars);

                var writer = new CharWriter(chars);
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                    state.CharsNeeded = 0;

                if (length != buffer.Length)
                    return encoding.GetByteCount(chars.Slice(0, writer.Length));
                else
                    return writer.Length;
#else
                var chars = new char[buffer.Length];
                var length = encoding.GetChars(buffer.ToArray(), 0, buffer.Length, chars, 0);

                var writer = new CharWriter(chars);
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                    state.CharsNeeded = 0;

                if (length != buffer.Length)
                    return encoding.GetByteCount(chars, 0, writer.Length);
                else
                    return writer.Length;
#endif
            }
            finally
            {
                Array.Clear(bufferCharOwner, 0, bufferCharOwner.Length);
                BufferArrayPool<char>.Return(bufferCharOwner);
            }
        }
    }
}
