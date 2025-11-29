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
        public static JsonObject? DeserializeJsonObject(string? str, JsonSerializerOptions? options = null, Graph? graph = null)
            => DeserializeJsonObject(str.AsSpan(), options, graph);
#endif

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
                throw new EndOfStreamException();

            return result;
        }

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
                throw new EndOfStreamException();

            return result;
        }

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
                        throw new EndOfStreamException();

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
                        throw new EndOfStreamException();

                    state.SizeNeeded = 0;
                }

                return result;
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

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
                        throw new EndOfStreamException();

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
                        throw new EndOfStreamException();

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
            var reader = new JsonReader(buffer, state.IsFinalBlock);
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
#if DEBUG
            if (!read && JsonReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<char> buffer, ref ReadState state, out JsonObject? result)
        {
            var reader = new JsonReader(buffer, state.IsFinalBlock);
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
#if DEBUG
            if (!read && JsonReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
    }
}
