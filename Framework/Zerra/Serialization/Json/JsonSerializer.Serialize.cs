﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Serialization.Json.IO;
using Zerra.Reflection;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using System.Text;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        private static ReadOnlyMemory<byte> nullBytes = Encoding.UTF8.GetBytes("null");

        public static string? Serialize<T>(T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj == null)
                return "null";

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                Graph = graph
            };

            var result = Write(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static string? Serialize(object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj == null)
                return "null";

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                Graph = graph
            };

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }
        public static string? Serialize(object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return "null";

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                Graph = graph
            };

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static byte[] SerializeBytes<T>(T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj == null)
                return nullBytes.ToArray();

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                Graph = graph
            };

            var result = WriteBytes(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static byte[] SerializeBytes(object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj == null)
                return nullBytes.ToArray();

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                Graph = graph
            };

            var result = WriteBoxedBytes(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }
        public static byte[] SerializeBytes(object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return nullBytes.ToArray();

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                Graph = graph
            };

            var result = WriteBoxedBytes(converter, defaultBufferSize, ref state, obj);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static void Serialize<T>(Stream stream, T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
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
                    EnumAsNumber = options.EnumAsNumber,
                    Graph = graph
                };

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
        public static void Serialize(Stream stream, object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
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
                    EnumAsNumber = options.EnumAsNumber,
                    Graph = graph
                };

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
        public static void Serialize(Stream stream, object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
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
                    EnumAsNumber = options.EnumAsNumber,
                    Graph = graph
                };

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

        public static async Task SerializeAsync<T>(Stream stream, T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
            {
#if NETSTANDARD2_0
                await stream.WriteAsync(nullBytes.ToArray(), 0, nullBytes.Length);
#else
                await stream.WriteAsync(nullBytes);
#endif
                return;
            }

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
                    EnumAsNumber = options.EnumAsNumber,
                    Graph = graph
                };

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
        public static async Task SerializeAsync(Stream stream, object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
            {
#if NETSTANDARD2_0
                await stream.WriteAsync(nullBytes.ToArray(), 0, nullBytes.Length);
#else
                await stream.WriteAsync(nullBytes);
#endif
                return;
            }

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
                    EnumAsNumber = options.EnumAsNumber,
                    Graph = graph
                };

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
        public static async Task SerializeAsync(Stream stream, object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
            {
#if NETSTANDARD2_0
                await stream.WriteAsync(nullBytes.ToArray(), 0, nullBytes.Length);
#else
                await stream.WriteAsync(nullBytes);
#endif
                return;
            }

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
                    EnumAsNumber = options.EnumAsNumber,
                    Graph = graph
                };

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
            var writer = new JsonWriter(false, initialSize);
            try
            {
#if DEBUG
            again:
#endif
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                {
                    state.CharsNeeded = 0;
                }
                else if (state.CharsNeeded == 0)
                {
#if DEBUG
                    throw new Exception($"{nameof(state.CharsNeeded)} not indicated");
#else
                    state.CharsNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && JsonWriter.Testing && writer.Position + state.CharsNeeded <= writer.Length)
                    goto again;
#endif
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
            var writer = new JsonWriter(false, initialSize);
            try
            {
#if DEBUG
            again:
#endif
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                {
                    state.CharsNeeded = 0;
                }
                else if (state.CharsNeeded == 0)
                {
#if DEBUG
                    throw new Exception($"{nameof(state.CharsNeeded)} not indicated");
#else
                    state.CharsNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && JsonWriter.Testing && writer.Position + state.CharsNeeded <= writer.Length)
                    goto again;
#endif
                var result = writer.ToString();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] WriteBytes<T>(JsonConverter<object, T> converter, int initialSize, ref WriteState state, T value)
        {
            var writer = new JsonWriter(true, initialSize);
            try
            {
#if DEBUG
            again:
#endif
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                {
                    state.CharsNeeded = 0;
                }
                else if (state.CharsNeeded == 0)
                {
#if DEBUG
                    throw new Exception($"{nameof(state.CharsNeeded)} not indicated");
#else
                    state.CharsNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && JsonWriter.Testing && writer.Position + state.CharsNeeded <= writer.Length)
                    goto again;
#endif
                var result = writer.ToByteArray();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] WriteBoxedBytes(JsonConverter<object> converter, int initialSize, ref WriteState state, object value)
        {
            var writer = new JsonWriter(true, initialSize);
            try
            {
#if DEBUG
            again:
#endif
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                {
                    state.CharsNeeded = 0;
                }
                else if (state.CharsNeeded == 0)
                {
#if DEBUG
                    throw new Exception($"{nameof(state.CharsNeeded)} not indicated");
#else
                    state.CharsNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && JsonWriter.Testing && writer.Position + state.CharsNeeded <= writer.Length)
                    goto again;
#endif
                var result = writer.ToByteArray();
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
            var writer = new JsonWriter(buffer);
            try
            {
#if DEBUG
            again:
#endif
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                {
                    state.CharsNeeded = 0;
                }
                else if (state.CharsNeeded == 0)
                {
#if DEBUG
                    throw new Exception($"{nameof(state.CharsNeeded)} not indicated");
#else
                    state.CharsNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && JsonWriter.Testing && writer.Position + state.CharsNeeded <= writer.Length)
                    goto again;
#endif
                return writer.Position;
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteBoxed(JsonConverter<object> converter, Span<byte> buffer, ref WriteState state, object value)
        {
            var writer = new JsonWriter(buffer);
            try
            {
#if DEBUG
            again:
#endif
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                {
                    state.CharsNeeded = 0;
                }
                else if (state.CharsNeeded == 0)
                {
#if DEBUG
                    throw new Exception($"{nameof(state.CharsNeeded)} not indicated");
#else
                    state.CharsNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && JsonWriter.Testing && writer.Position + state.CharsNeeded <= writer.Length)
                    goto again;
#endif
                return writer.Position;
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
