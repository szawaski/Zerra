// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Zerra.IO;
using Zerra.Serialization.Json.Converters;
using Zerra.Serialization.Json.State;
using Zerra.Serialization.Json.IO;

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
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            if (chars.Length == 0)
                return new JsonObject();

            options ??= defaultOptions;

            var state = new ReadState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                Graph = graph,

                IsFinalBlock = true
            };

            JsonObject? result;

            Read(chars, ref state, out result);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static JsonObject? DeserializeJsonObject(ReadOnlySpan<byte> bytes, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0)
                return new JsonObject();

            options ??= defaultOptions;

            var state = new ReadState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                Graph = graph,

                IsFinalBlock = true
            };

            JsonObject? result;

            Read(bytes, ref state, out result);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static JsonObject? DeserializeJsonObject(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var isFinalBlock = false;
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

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
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                    Graph = graph,

                    IsFinalBlock = isFinalBlock
                };

                JsonObject? result;

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, out result);

                    if (state.CharsNeeded == 0)
                        break;

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    position = length - position;

                    if (position + state.CharsNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, position + state.CharsNeeded);

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

                    if (position < state.CharsNeeded)
                        throw new EndOfStreamException();

                    state.CharsNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        public static async Task<JsonObject?> DeserializeJsonObjectAsync(Stream stream, JsonSerializerOptions? options = null, Graph? graph = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var isFinalBlock = false;
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var position = 0;
                var length = 0;
                var read = -1;
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

                if (position == 0)
                {
                    return new JsonObject();
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,
                    Graph = graph,

                    IsFinalBlock = isFinalBlock
                };

                JsonObject? result;

                for (; ; )
                {
                    var usedBytes = Read(buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.CharsNeeded == 0)
                        break;

                    if (state.IsFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                    position = length - usedBytes;

                    if (position + state.CharsNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, position + state.CharsNeeded);

                    while (position < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, position, buffer.Length - position);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(position));
#endif

                        if (read == 0)
                        {
                            state.IsFinalBlock = true;
                            break;
                        }
                        position += read;
                        length = position;
                    }

                    if (position < state.CharsNeeded)
                        throw new EndOfStreamException();

                    state.CharsNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<byte> buffer, ref ReadState state, out JsonObject? result)
        {
            var reader = new JsonReader(buffer);
#if DEBUG
        again:
#endif
            var read = JsonConverter.TryReadJsonObject(ref reader, ref state, out result);
            if (read)
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
            if (!read && JsonReader.Testing && reader.Position + state.CharsNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<char> buffer, ref ReadState state, out JsonObject? result)
        {
            var reader = new JsonReader(buffer);
#if DEBUG
        again:
#endif
            var read = JsonConverter.TryReadJsonObject(ref reader, ref state, out result);
            if (read)
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
            if (!read && JsonReader.Testing && reader.Position + state.CharsNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
    }
}
