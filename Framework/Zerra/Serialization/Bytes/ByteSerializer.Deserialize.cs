// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using Zerra.Buffers;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.Converters;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes
{
    public static partial class ByteSerializer
    {
        /// <summary>
        /// Deserializes a byte span to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="bytes">The byte span containing the serialized data. If empty, returns the default value for the type.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <returns>The deserialized object, or the default value if the byte span is empty.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
                throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

            return result;
        }
        /// <summary>
        /// Deserializes a byte span to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <param name="bytes">The byte span containing the serialized data. If empty, returns null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <returns>The deserialized object, or null if the byte span is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
        public static object? Deserialize(ReadOnlySpan<byte> bytes, Type type, ByteSerializerOptions? options = null)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (bytes.Length == 0)
                return default;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var state = new ReadState(options);
            object? result;

            _ = ReadBoxed(converter, bytes, ref state, out result);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

            return result;
        }

        /// <summary>
        /// Deserializes data from a stream to an object of the specified type using the default or provided deserialization options.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="stream">The stream containing the serialized data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <returns>The deserialized object, or the default value if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
                        {
                            BufferShift(buffer, bytesUsed);
                            length -= bytesUsed;
#if NETSTANDARD2_0
                            read = stream.Read(buffer, length, buffer.Length - length);
#else
                            read = stream.Read(buffer.AsSpan(length));
#endif
                            if (read != 0)
                                throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");
                        }
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
                            isFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < totalSizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
        /// <param name="stream">The stream containing the serialized data. Must not be null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <returns>The deserialized object, or null if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
                        {
                            BufferShift(buffer, bytesUsed);
                            length -= bytesUsed;
#if NETSTANDARD2_0
                            read = stream.Read(buffer, length, buffer.Length - length);
#else
                            read = stream.Read(buffer.AsSpan(length));
#endif
                            if (read != 0)
                                throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");
                        }
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
                            isFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < totalSizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
        /// <param name="stream">The stream containing the serialized data. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns the deserialized object, or the default value if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
                        {
                            BufferShift(buffer, bytesUsed);
                            length -= bytesUsed;
#if NETSTANDARD2_0
                            read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                            read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif
                            if (read != 0)
                                throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");
                        }
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
                            isFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < totalSizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
        /// <param name="stream">The stream containing the serialized data. Must not be null.</param>
        /// <param name="type">The type to deserialize to. Must not be null.</param>
        /// <param name="options">Optional deserialization options. If null, uses default options.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous deserialization operation and returns the deserialized object, or null if the stream is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if the data is invalid or incomplete.</exception>
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
                        {
                            BufferShift(buffer, bytesUsed);
                            length -= bytesUsed;
#if NETSTANDARD2_0
                            read = await stream.ReadAsync(buffer, length, buffer.Length - length, cancellationToken);
#else
                            read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
#endif
                            if (read != 0)
                                throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");
                        }
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
                            isFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < totalSizeNeeded)
                        throw new EndOfStreamException($"Invalid data for {nameof(ByteSerializer)} or the stream ended early");

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
            if (!read && ByteReader.Testing && reader.Alternate)
                goto again;
            if (!read && reader.Position + state.SizeNeeded <= reader.Length)
                System.Diagnostics.Debugger.Break();
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
            if (!read && ByteReader.Testing && reader.Alternate)
                goto again;
            if (!read && reader.Position + state.SizeNeeded <= reader.Length)
                System.Diagnostics.Debugger.Break();
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