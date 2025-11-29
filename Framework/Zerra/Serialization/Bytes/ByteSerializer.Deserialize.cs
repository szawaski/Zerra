// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Buffers;
using Zerra.Serialization.Bytes.Converters;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;
using Zerra.SourceGeneration;

namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializer
    {
        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes, ByteSerializerOptions? options = null)
        {
            if (bytes.Length == 0)
                return default;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

            var state = new ReadState(options);
            T? result;

            _ = Read(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static object? Deserialize(ReadOnlySpan<byte> bytes, Type type, ByteSerializerOptions? options = null)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var state = new ReadState(options);
            object? result;

            _ = ReadBoxed(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static T? Deserialize<T>(Stream stream, ByteSerializerOptions? options = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState(options);
                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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
        public static object? Deserialize(Stream stream, Type type, ByteSerializerOptions? options = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState(options);
                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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

        public static async Task<T?> DeserializeAsync<T>(Stream stream, ByteSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState(options);
                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    if (state.SizeNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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
        public static async Task<object?> DeserializeAsync(Stream stream, Type type, ByteSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState(options);
                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, out result);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (state.SizeNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
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
                            isFinalBlock = true;
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
        private static int Read<T>(ByteConverter<T> converter, ReadOnlySpan<byte> buffer, ref ReadState state, out T? result)
        {
            var reader = new ByteReader(buffer);
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
                if (!ByteWriter.Testing)
                    throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!read && ByteReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(ByteConverter converter, ReadOnlySpan<byte> buffer, ref ReadState state, out object? result)
        {
            var reader = new ByteReader(buffer);
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
                if (!ByteWriter.Testing)
                    throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!read && ByteReader.Testing && reader.Position + state.SizeNeeded <= reader.Length)
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