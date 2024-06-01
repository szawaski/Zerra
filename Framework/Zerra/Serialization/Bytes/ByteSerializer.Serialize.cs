// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zerra.IO;
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
            if (obj == null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<object, T>)ByteConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                IncludeTypes = options.UseTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16,
            };
            state.PushFrame(true);

            var result = Write(converter, defaultBufferSize, ref state, options.Encoding, obj);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static byte[] Serialize(object? obj, ByteSerializerOptions? options = null)
        {
            if (obj == null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                IncludeTypes = options.UseTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16,
            };
            state.PushFrame(true);

            var result = WriteBoxed(converter, defaultBufferSize, ref state, options.Encoding, obj);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;

        }
        public static byte[] Serialize(object? obj, Type type, ByteSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return Array.Empty<byte>();

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var state = new WriteState()
            {
                IncludeTypes = options.UseTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
            state.PushFrame(true);

            var result = WriteBoxed(converter, defaultBufferSize, ref state, options.Encoding, obj);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static void Serialize<T>(Stream stream, T? obj, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<object, T>)ByteConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    IncludeTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);

                for (; ; )
                {
                    var usedBytes = Write(converter, buffer, ref state, options.Encoding, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static void Serialize(Stream stream, object? obj, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    IncludeTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, options.Encoding, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static void Serialize(Stream stream, object? obj, Type type, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    IncludeTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, options.Encoding, obj);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        public static async Task SerializeAsync<T>(Stream stream, T? obj, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<object, T>)ByteConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    IncludeTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);

                for (; ; )
                {
                    var usedBytes = Write(converter, buffer, ref state, options.Encoding, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static async Task SerializeAsync(Stream stream, object? obj, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    IncludeTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, options.Encoding, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static async Task SerializeAsync(Stream stream, object? obj, Type type, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return;

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);

            try
            {
                var state = new WriteState()
                {
                    IncludeTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);

                for (; ; )
                {
                    var usedBytes = WriteBoxed(converter, buffer, ref state, options.Encoding, obj);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.BytesNeeded == 0)
                        break;

                    if (state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                    state.BytesNeeded = 0;
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] Write<T>(ByteConverter<object, T> converter, int initialSize, ref WriteState state, Encoding encoding, T value)
        {
            var writer = new ByteWriter(initialSize, encoding);
            try
            {
                var write = converter.TryWrite(ref writer, ref state, value);
                if (write)
                    state.BytesNeeded = 0;
                var result = writer.ToArray();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] WriteBoxed(ByteConverter<object> converter, int initialSize, ref WriteState state, Encoding encoding, object value)
        {
            var writer = new ByteWriter(initialSize, encoding);
            try
            {
                var write = converter.TryWriteBoxed(ref writer, ref state, value);
                if (write)
                    state.BytesNeeded = 0;
                var result = writer.ToArray();
                return result;
            }
            finally
            {
                writer.Dispose();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Write<T>(ByteConverter<object, T> converter, Span<byte> buffer, ref WriteState state, Encoding encoding, T value)
        {
            var writer = new ByteWriter(buffer, encoding);
            var write = converter.TryWrite(ref writer, ref state, value);
            if (write)
                state.BytesNeeded = 0;
            return writer.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int WriteBoxed(ByteConverter<object> converter, Span<byte> buffer, ref WriteState state, Encoding encoding, object value)
        {
            var writer = new ByteWriter(buffer, encoding);
            var write = converter.TryWriteBoxed(ref writer, ref state, value);
            if (write)
                state.BytesNeeded = 0;
            return writer.Length;
        }
    }
}