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

            var state = new ReadState()
            {
                Nameless = options.Nameless,
                EnumAsNumber = options.EnumAsNumber,
                ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                Graph = graph,

                IsFinalBlock = true
            };

            JsonObject? result;

            _ = Read(chars, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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

            var state = new ReadState()
            {
                Nameless = options.Nameless,
                EnumAsNumber = options.EnumAsNumber,
                ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                Graph = graph,

                IsFinalBlock = true
            };

            JsonObject? result;

            _ = Read(bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

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
                var position = 0;
                var length = 0;
                var read = -1;
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
                    return new JsonObject();
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                    Graph = graph,

                    IsFinalBlock = isFinalBlock
                };

                JsonObject? result;

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, out result);

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    position = length - position;

                    if (position + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, position + state.SizeNeeded);

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

                    if (position < state.SizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    state.SizeNeeded = 0;
                }

                return result;
            }
            finally
            {
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
                var position = 0;
                var length = 0;
                var read = -1;
                while (position < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, position, buffer.Length - position, cancellationToken);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(position), cancellationToken);
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
                    return new JsonObject();
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                    Graph = graph,

                    IsFinalBlock = isFinalBlock
                };

                JsonObject? result;

                for (; ; )
                {
                    var usedBytes = Read(buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                        break;

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                    position = length - usedBytes;

                    if (position + state.SizeNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, position + state.SizeNeeded);

                    while (position < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, position, buffer.Length - position, cancellationToken);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(position), cancellationToken);
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        position += read;
                        length = position;
                    }

                    if (position < state.SizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(JsonSerializer)} or the stream ended early");

                    state.SizeNeeded = 0;
                }

                return result;
            }
            finally
            {
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
