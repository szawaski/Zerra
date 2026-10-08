// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;
using Zerra.Buffers;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
#if NETSTANDARD2_0
        /// <summary>
        /// Deserializes a JSON string to a <see cref="JsonObject"/> using the default or provided deserialization options.
        /// </summary>
        /// <param name="str">The JSON string containing the serialized data. If null or empty, returns an empty JsonObject.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JsonObject representing the deserialized JSON data, or an empty JsonObject if the string is null or empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static JsonObject? DeserializeJsonObject(string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => DeserializeJsonObject(str.AsSpan(), options, graph);
#endif

        /// <summary>
        /// Deserializes a JSON character span to a <see cref="JsonObject"/> using the default or provided deserialization options.
        /// </summary>
        /// <param name="chars">The character span containing the JSON data. If empty, returns an empty JsonObject.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JsonObject representing the deserialized JSON data, or an empty JsonObject if the span is empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static JsonObject? DeserializeJsonObject(ReadOnlySpan<char> chars, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (chars.Length == 0)
                return new JsonObject();

            options ??= defaultOptions;

            var state = new ReadState(options, graph, true, false);

            JsonObject? result;

            var used = Read(chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
            ThrowIfNotWhitespace(chars.Slice(used));

            return result;
        }

        /// <summary>
        /// Deserializes a UTF-8 byte span to a <see cref="JsonObject"/> using the default or provided deserialization options.
        /// </summary>
        /// <param name="bytes">The byte span containing the JSON data. If empty, returns an empty JsonObject.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JsonObject representing the deserialized JSON data, or an empty JsonObject if the span is empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static JsonObject? DeserializeJsonObject(ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (bytes.Length == 0)
                return new JsonObject();

            options ??= defaultOptions;

            var state = new ReadState(options, graph, true, false);

            JsonObject? result;

            var used = Read(bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");
            ThrowIfNotWhitespace(bytes.Slice(used));

            return result;
        }

        /// <summary>
        /// Deserializes data from a stream to a <see cref="JsonObject"/> using the default or provided deserialization options.
        /// </summary>
        /// <param name="stream">The stream containing the JSON data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <returns>A JsonObject representing the deserialized JSON data, or an empty JsonObject if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static JsonObject? DeserializeJsonObject(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

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
                    return new JsonObject();

                var state = new ReadState(options, graph, isFinalBlock, false);

                JsonObject? result;

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        ThrowIfNotWhitespace(buffer.AsSpan(bytesUsed, length - bytesUsed));
                        if (!state.IsFinalBlock)
                        {
                            for (; ; )
                            {
#if NETSTANDARD2_0
                                read = stream.Read(buffer, 0, buffer.Length);
#else
                                read = stream.Read(buffer.AsSpan());
#endif
                                if (read == 0)
                                    break;
                                ThrowIfNotWhitespace(buffer.AsSpan(0, read));
                            }
                        }
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    var totalSizeNeeded = length + state.SizeNeeded;
                    if (totalSizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, totalSizeNeeded);

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
        /// Asynchronously deserializes data from a stream to a <see cref="JsonObject"/> using the default or provided deserialization options.
        /// </summary>
        /// <param name="stream">The stream containing the JSON data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="graph">Optional graph for handling circular references. If null, no circular reference detection is performed.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns a JsonObject representing the deserialized JSON data, or an empty JsonObject if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static async Task<JsonObject?> DeserializeJsonObjectAsync(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

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
                    return new JsonObject();

                var state = new ReadState(options, graph, isFinalBlock, false);

                JsonObject? result;

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        ThrowIfNotWhitespace(buffer.AsSpan(bytesUsed, length - bytesUsed));
                        if (!state.IsFinalBlock)
                        {
                            for (; ; )
                            {
#if NETSTANDARD2_0
                                read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
#else
                                read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
#endif
                                if (read == 0)
                                    break;
                                ThrowIfNotWhitespace(buffer.AsSpan(0, read));
                            }
                        }
                        break;
                    }

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    BufferShift(buffer, bytesUsed);
                    length -= bytesUsed;

                    var totalSizeNeeded = length + state.SizeNeeded;
                    if (totalSizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, totalSizeNeeded);

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
        private static int Read(ReadOnlySpan<byte> buffer, ref ReadState state, out JsonObject? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock, state.LastReaderToken);
#if DEBUG
        again:
#endif
            var read = JsonConverter.TryReadJsonObject(ref reader, ref state, out result);
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
            else
            {
                state.LastReaderToken = reader.Token;
            }
#if DEBUG
            if (!read && JsonReader.Testing && reader.Alternate)
            {
                state.LastReaderToken = reader.Token;
                goto again;
            }
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<char> buffer, ref ReadState state, out JsonObject? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock, state.LastReaderToken);
#if DEBUG
        again:
#endif
            var read = JsonConverter.TryReadJsonObject(ref reader, ref state, out result);
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
            else
            {
                state.LastReaderToken = reader.Token;
            }
#if DEBUG
            if (!read && JsonReader.Testing && reader.Alternate)
            {
                state.LastReaderToken = reader.Token;
                goto again;
            }
#endif
            return reader.Position;
        }
    }
}
