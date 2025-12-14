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
        /// Serializes an object to a byte array using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. If null, returns an empty byte array.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <returns>A byte array containing the serialized data, or an empty array if the object is null.</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static byte[] Serialize<T>(T? obj, ByteSerializerOptions? options = null)
        {
            if (obj is null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

            var state = new WriteState(options);

            var result = Write(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        /// <summary>
        /// Serializes an object to a byte array using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="obj">The object to serialize. If null, returns an empty byte array.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <returns>A byte array containing the serialized data, or an empty array if the object is null.</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static byte[] Serialize(object? obj, ByteSerializerOptions? options = null)
        {
            if (obj is null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var state = new WriteState(options);

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }
        /// <summary>
        /// Serializes an object to a byte array using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="obj">The object to serialize. If null, returns an empty byte array.</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <returns>A byte array containing the serialized data, or an empty array if the object is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if type is null.</exception>
        /// <exception cref="EndOfStreamException">Thrown if there is insufficient space for serialization.</exception>
        public static byte[] Serialize(object? obj, Type type, ByteSerializerOptions? options = null)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var state = new WriteState(options);

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.SizeNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        /// <summary>
        /// Serializes an object to a stream using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="stream">The stream to write the serialized data to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static void Serialize<T>(Stream stream, T? obj, ByteSerializerOptions? options = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options);

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
        /// Serializes an object to a stream using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static void Serialize(Stream stream, object? obj, ByteSerializerOptions? options = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options);

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
        /// Serializes an object to a stream using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        public static void Serialize(Stream stream, object? obj, Type type, ByteSerializerOptions? options = null)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options);

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
        /// Asynchronously serializes an object to a stream using the default or provided serialization options.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="stream">The stream to write the serialized data to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static async Task SerializeAsync<T>(Stream stream, T? obj, ByteSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options);

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
        /// Asynchronously serializes an object to a stream using its runtime type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream is null.</exception>
        public static async Task SerializeAsync(Stream stream, object? obj, ByteSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options);

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
        /// Asynchronously serializes an object to a stream using the specified type and the default or provided serialization options.
        /// </summary>
        /// <param name="stream">The stream to write the serialized data to. Must not be null.</param>
        /// <param name="obj">The object to serialize. If null, no data is written.</param>
        /// <param name="type">The type to use for serialization. Must not be null.</param>
        /// <param name="options">Optional serialization options. If null, uses default options.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous serialization operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if stream or type is null.</exception>
        public static async Task SerializeAsync(Stream stream, object? obj, Type type, ByteSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (obj is null)
                return;

            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (obj is null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState(options);

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
        private static byte[] Write<T>(ByteConverter<T> converter, int initialSize, ref WriteState state, T value)
        {
            var writer = new ByteWriter(initialSize);
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
                    if (!ByteWriter.Testing)
                        throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                    state.SizeNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && ByteWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
                    goto again;
#endif
                var result = writer.ToArray();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] WriteBoxed(ByteConverter converter, int initialSize, ref WriteState state, object value)
        {
            var writer = new ByteWriter(initialSize);
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
                    if (!ByteWriter.Testing)
                        throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                    state.SizeNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && ByteWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
                    goto again;
#endif
                var result = writer.ToArray();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Write<T>(ByteConverter<T> converter, Span<byte> buffer, ref WriteState state, T value)
        {
            var writer = new ByteWriter(buffer);
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
                if (!ByteWriter.Testing)
                    throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!write && ByteWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
                goto again;
#endif
            return writer.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteBoxed(ByteConverter converter, Span<byte> buffer, ref WriteState state, object value)
        {
            var writer = new ByteWriter(buffer);
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
                if (!ByteWriter.Testing)
                    throw new Exception($"{nameof(state.SizeNeeded)} not indicated");
#else
                state.SizeNeeded = 1;
#endif
            }
#if DEBUG
            if (!write && ByteWriter.Testing && writer.Position + state.SizeNeeded <= writer.Length)
                goto again;
#endif
            return writer.Position;
        }
    }
}