// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.IO;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using System.Text;
using Zerra.Buffers;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        private static ReadOnlyMemory<byte> nullBytes = Encoding.UTF8.GetBytes("null");

        /// <summary>
        /// Serializes an object to a JSON string using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. If null, returns "null".</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JSON string representation of the object, or "null" if the object is null.</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static string Serialize<T>(T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj is null)
                return "null";

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var state = new WriteState(options, graph?.GetInstanceGraph(obj));

            var result = Write(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        /// <summary>
        /// Serializes an object to a JSON string using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="obj">The object to serialize. If null, returns "null".</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JSON string representation of the object, or "null" if the object is null.</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static string Serialize(object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj is null)
                return "null";

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new WriteState(options, graph?.GetInstanceGraph(obj));

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }

        /// <summary>
        /// Serializes an object to a JSON string using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="obj">The object to serialize. If null, returns "null".</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JSON string representation of the object, or "null" if the object is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static string Serialize(object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
                return "null";

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new WriteState(options, graph?.GetInstanceGraph(obj));

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        /// <summary>
        /// Serializes an object to a UTF-8 encoded byte array using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. If null, returns the UTF-8 encoding of "null".</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A byte array containing the JSON representation of the object, or the encoding of "null" if the object is null.</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static byte[] SerializeBytes<T>(T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj is null)
                return nullBytes.ToArray();

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var state = new WriteState(options, graph?.GetInstanceGraph(obj));

            var result = WriteBytes(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        /// <summary>
        /// Serializes an object to a UTF-8 encoded byte array using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="obj">The object to serialize. If null, returns the UTF-8 encoding of "null".</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A byte array containing the JSON representation of the object, or the encoding of "null" if the object is null.</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static byte[] SerializeBytes(object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (obj is null)
                return nullBytes.ToArray();

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new WriteState(options, graph?.GetInstanceGraph(obj));

            var result = WriteBoxedBytes(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }

        /// <summary>
        /// Serializes an object to a UTF-8 encoded byte array using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="obj">The object to serialize. If null, returns the UTF-8 encoding of "null".</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A byte array containing the JSON representation of the object, or the encoding of "null" if the object is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static byte[] SerializeBytes(object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
                return nullBytes.ToArray();

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new WriteState(options, graph?.GetInstanceGraph(obj));

            var result = WriteBoxedBytes(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        /// <summary>
        /// Serializes an object to a stream as JSON using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="stream">The stream to write the JSON to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static void Serialize<T>(Stream stream, T? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options, graph?.GetInstanceGraph(obj));

                for (; ; )
                {
                    var usedBytes = Write(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.SizeNeeded);

                    state.SizeNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        /// <summary>
        /// Serializes an object to a stream as JSON using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the JSON to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static void Serialize(Stream stream, object? obj, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options, graph?.GetInstanceGraph(obj));

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.SizeNeeded);

                    state.SizeNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        /// <summary>
        /// Serializes an object to a stream as JSON using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the JSON to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        public static void Serialize(Stream stream, object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options, graph?.GetInstanceGraph(obj));

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.SizeNeeded);

                    state.SizeNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously serializes an object to a stream as JSON using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="stream">The stream to write the JSON to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, writes "null" to the stream.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static async Task SerializeAsync<T>(Stream stream, T? obj, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
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
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options, graph?.GetInstanceGraph(obj));

                for (; ; )
                {
                    var usedBytes = Write(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes, cancellationToken);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes), cancellationToken);
#endif

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.SizeNeeded);

                    state.SizeNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously serializes an object to a stream as JSON using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the JSON to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, writes "null" to the stream.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static async Task SerializeAsync(Stream stream, object? obj, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
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
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options, graph?.GetInstanceGraph(obj));

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes, cancellationToken);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes), cancellationToken);
#endif

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.SizeNeeded);

                    state.SizeNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        /// <summary>
        /// Asynchronously serializes an object to a stream as JSON using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the JSON to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, writes "null" to the stream.</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        public static async Task SerializeAsync(Stream stream, object? obj, Type type, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
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
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options, graph?.GetInstanceGraph(obj));

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes, cancellationToken);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes), cancellationToken);
#endif

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.SizeNeeded);

                    state.SizeNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string Write<T>(JsonConverter<T> converter, int initialSize, ref WriteState state, T value)
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
                if (!write && JsonWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
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
        private static string WriteBoxed(JsonConverter converter, int initialSize, ref WriteState state, object value)
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
                if (!write && JsonWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
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
        private static byte[] WriteBytes<T>(JsonConverter<T> converter, int initialSize, ref WriteState state, T value)
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
                if (!write && JsonWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
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
        private static byte[] WriteBoxedBytes(JsonConverter converter, int initialSize, ref WriteState state, object value)
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
                if (!write && JsonWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
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
        private static int Write<T>(JsonConverter<T> converter, Span<byte> buffer, ref WriteState state, T value)
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
                if (!write && JsonWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
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
        private static int WriteBoxed(JsonConverter converter, Span<byte> buffer, ref WriteState state, object value)
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
                if (!write && JsonWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
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
