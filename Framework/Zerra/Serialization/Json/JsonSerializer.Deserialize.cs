// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;
using Zerra.Buffers;
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
#if NETSTANDARD2_0
        /// <summary>
        /// Deserializes a JSON string to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="str">The JSON string containing the serialized data. If null or empty, returns the default value for the type.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or the default value if the string is null or empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static T? Deserialize<T>(string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => Deserialize<T>(str.AsSpan(), options, graph);

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="str">The JSON string containing the serialized data. If null or empty, returns null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or null if the string is null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static object? Deserialize(Type type, string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => Deserialize(str.AsSpan(), type, options, graph);
#endif

        /// <summary>
        /// Deserializes a JSON character span to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="chars">The character span containing the JSON data. If empty, returns the default value for the type or an empty string if T is string.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or the default value if the span is empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, false);

            T? result;

            _ = Read(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return result;
        }
        /// <summary>
        /// Deserializes a JSON character span to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <param name="chars">The character span containing the JSON data. If empty, returns null or an empty string if type is string.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or null if the span is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static object? Deserialize(ReadOnlySpan<char> chars, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
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
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, false);

            object? result;

            _ = ReadBoxed(converter, chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return result;
        }

        /// <summary>
        /// Deserializes a UTF-8 byte span to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="bytes">The byte span containing the JSON data. If empty, returns the default value for the type or an empty string if T is string.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or the default value if the span is empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, false);

            T? result;

            _ = Read(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return result;
        }
        /// <summary>
        /// Deserializes a UTF-8 byte span to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <param name="bytes">The byte span containing the JSON data. If empty, returns null or an empty string if type is string.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or null if the span is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static object? Deserialize(ReadOnlySpan<byte> bytes, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
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
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

            var state = new ReadState(options, graph, true, false);

            object? result;

            _ = ReadBoxed(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

            return result;
        }

        /// <summary>
        /// Deserializes data from a stream to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing the JSON data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or the default value if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static T? Deserialize<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

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

                var state = new ReadState(options, graph, isFinalBlock, false);

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
        /// <summary>
        /// Deserializes data from a stream to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <param name="stream">The stream containing the JSON data. Must not be null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>The deserialized object, or null if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static object? Deserialize(Stream stream, Type type, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

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

                var state = new ReadState(options, graph, isFinalBlock, false);

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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

        /// <summary>
        /// Asynchronously deserializes data from a stream to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing the JSON data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns the deserialized object, or the default value if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static async Task<T?> DeserializeAsync<T>(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<T>)JsonConverterFactory.CreateRoot(typeDetail);

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

                var state = new ReadState(options, graph, isFinalBlock, false);

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
        /// <summary>
        /// Asynchronously deserializes data from a stream to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <param name="stream">The stream containing the JSON data. Must not be null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns the deserialized object, or null if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static async Task<object?> DeserializeAsync(Stream stream, Type type, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory.CreateRoot(typeDetail);

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

                var state = new ReadState(options, graph, isFinalBlock, false);

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!state.IsFinalBlock)
                            throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
        private static int Read<T>(JsonConverter<T> converter, ReadOnlySpan<byte> buffer, ref ReadState state, out T? result)
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
        private static int Read<T>(JsonConverter<T> converter, ReadOnlySpan<char> buffer, ref ReadState state, out T? result)
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
