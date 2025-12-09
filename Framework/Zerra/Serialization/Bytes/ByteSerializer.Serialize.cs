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
        public static byte[] Serialize<T>(T? obj, ByteSerializerOptions? options = null)
        {
            if (obj is null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<T>)ByteConverterFactory.GetRoot(typeDetail);

            var state = new WriteState(options);

            var result = Write(converter, defaultBufferSize, ref state, obj);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static byte[] Serialize(object? obj, ByteSerializerOptions? options = null)
        {
            if (obj is null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory.GetRoot(typeDetail);

            var state = new WriteState(options);

            var result = WriteBoxed(converter, defaultBufferSize, ref state, obj);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }
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

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

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

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
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

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
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

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

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

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
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

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
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

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
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
                    state.BytesNeeded = 0;
                }
                else if (state.BytesNeeded == 0)
                {
#if DEBUG
                    if (!ByteWriter.Testing)
                        throw new Exception($"{nameof(state.BytesNeeded)} not indicated");
#else
                    state.BytesNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && ByteWriter.Testing && writer.Position + state.BytesNeeded <= writer.Length)
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
                    state.BytesNeeded = 0;
                }
                else if (state.BytesNeeded == 0)
                {
#if DEBUG
                    if (!ByteWriter.Testing)
                        throw new Exception($"{nameof(state.BytesNeeded)} not indicated");
#else
                    state.BytesNeeded = 1;
#endif
                }
#if DEBUG
                if (!write && ByteWriter.Testing && writer.Position + state.BytesNeeded <= writer.Length)
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
                state.BytesNeeded = 0;
            }
            else if (state.BytesNeeded == 0)
            {
#if DEBUG
                if (!ByteWriter.Testing)
                    throw new Exception($"{nameof(state.BytesNeeded)} not indicated");
#else
                state.BytesNeeded = 1;
#endif
            }
#if DEBUG
            if (!write && ByteWriter.Testing && writer.Position + state.BytesNeeded <= writer.Length)
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
                state.BytesNeeded = 0;
            }
            else if (state.BytesNeeded == 0)
            {

#if DEBUG
                if (!ByteWriter.Testing)
                    throw new Exception($"{nameof(state.BytesNeeded)} not indicated");
#else
                state.BytesNeeded = 1;
#endif
            }
#if DEBUG
            if (!write && ByteWriter.Testing && writer.Position + state.BytesNeeded <= writer.Length)
                goto again;
#endif
            return writer.Position;
        }
    }
}