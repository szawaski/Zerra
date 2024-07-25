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
using Zerra.Reflection;

namespace Zerra.Serialization.Json
{
    public partial class JsonSerializer
    {
        public static T? Deserialize<T>(ReadOnlySpan<char> chars, JsonSerializerOptions? options = null)
        {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            if (chars.Length == 0)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)String.Empty;
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,

                IsFinalBlock = true
            };

            T? result;

            Read(converter, chars, ref state, out result);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static object? Deserialize(Type type, ReadOnlySpan<char> chars, JsonSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));
            if (chars.Length == 0)
            {
                if (type == typeof(string))
                    return String.Empty;
                return default;
            }

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState()
            {
                Nameless = options.Nameless,
                DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                EnumAsNumber = options.EnumAsNumber,
                ErrorOnTypeMismatch = options.ErrorOnTypeMismatch,

                IsFinalBlock = true
            };

            object? result;

            ReadBoxed(converter, chars, ref state, out result);

            if (state.CharsNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static T? Deserialize<T>(Stream stream, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

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
                    if (typeof(T) == typeof(string))
                        return (T)(object)String.Empty;
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch
                };

                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, read), ref state, out result);

                    if (state.CharsNeeded == 0)
                        break;

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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
        public static object? Deserialize(Type type, Stream stream, JsonSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

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
                    if (type == typeof(string))
                        return String.Empty;
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch
                };

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, read), ref state, out result);

                    if (state.CharsNeeded == 0)
                        break;

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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

        public static async Task<T?> DeserializeAsync<T>(Stream stream, JsonSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (JsonConverter<object, T>)JsonConverterFactory<object>.GetRoot(typeDetail);

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
                    if (typeof(T) == typeof(string))
                        return (T)(object)String.Empty;
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch
                };

                T? result;

                for (; ; )
                {
                    var usedBytes = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.CharsNeeded == 0)
                        break;

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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
        public static async Task<object?> DeserializeAsync(Type type, Stream stream, JsonSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = JsonConverterFactory<object>.GetRoot(typeDetail);

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
                    if (type == typeof(string))
                        return String.Empty;
                    return default;
                }

                var state = new ReadState()
                {
                    Nameless = options.Nameless,
                    DoNotWriteNullProperties = options.DoNotWriteNullProperties,
                    EnumAsNumber = options.EnumAsNumber,
                    ErrorOnTypeMismatch = options.ErrorOnTypeMismatch
                };

                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, read), ref state, out result);

                    if (state.CharsNeeded == 0)
                        break;

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    position = length - position;

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
                            isFinalBlock = true;
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
        private static int Read<T>(JsonConverter<object, T> converter, ReadOnlySpan<byte> buffer, ref ReadState state, out T? result)
        {
            var reader = new JsonReader(buffer);
            var read = converter.TryRead(ref reader, ref state, out result);
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
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(JsonConverter converter, ReadOnlySpan<byte> buffer, ref ReadState state, out object? result)
        {
            var reader = new JsonReader(buffer);
            var read = converter.TryReadBoxed(ref reader, ref state, out result);
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
            return reader.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read<T>(JsonConverter<object, T> converter, ReadOnlySpan<char> buffer, ref ReadState state, out T? result)
        {
            var reader = new JsonReader(buffer);
            var read = converter.TryRead(ref reader, ref state, out result);
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
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(JsonConverter converter, ReadOnlySpan<char> buffer, ref ReadState state, out object? result)
        {
            var reader = new JsonReader(buffer);
            var read = converter.TryReadBoxed(ref reader, ref state, out result);
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
            return reader.Position;
        }
    }
}
