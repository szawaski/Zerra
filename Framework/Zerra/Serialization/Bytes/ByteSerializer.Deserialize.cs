// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes, ByteSerializerOptions? options = null)
        {
            if (bytes == null) 
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0)
                return default;

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<object, T>)ByteConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState()
            {
                IncludePropertyTypes = options.IncludePropertyTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
            state.PushFrame(true);
            T? result;

            Read(converter, bytes, ref state, options.Encoding, out result);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }
        public static object? Deserialize(Type type, ReadOnlySpan<byte> bytes, ByteSerializerOptions? options = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState()
            {
                IncludePropertyTypes = options.IncludePropertyTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
            state.PushFrame(true);
            object? result;

            ReadBoxed(converter, bytes, ref state, options.Encoding, out result);

            if (state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return result;
        }

        public static T? Deserialize<T>(Stream stream, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<object, T>)ByteConverterFactory<object>.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState()
                {
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);
                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, read), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                        break;

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    position = length - position;

                    if (position + state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

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

                    if (position < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static object? Deserialize(Type type, Stream stream, ByteSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState()
                {
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);
                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, read), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                        break;

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    position = length - position;

                    if (position + state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

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

                    if (position < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        public static async Task<T?> DeserializeAsync<T>(Stream stream, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = (ByteConverter<object, T>)ByteConverterFactory<object>.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState()
                {
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);
                T? result;

                for (; ; )
                {
                    var usedBytes = Read(converter, buffer.AsSpan().Slice(0, length), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                        break;

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                    position = length - usedBytes;

                    if (position + state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

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

                    if (position < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }
        public static async Task<object?> DeserializeAsync(Type type, Stream stream, ByteSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

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
                    return default;
                }

                var state = new ReadState()
                {
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(true);
                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, read), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                        break;

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    position = length - position;

                    if (position + state.BytesNeeded > buffer.Length)
                        BufferArrayPool<byte>.Grow(ref buffer, position + state.BytesNeeded);

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

                    if (position < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
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
        private static int Read<T>(ByteConverter<object, T> converter, ReadOnlySpan<byte> buffer, ref ReadState state, Encoding encoding, out T? result)
        {
            var reader = new ByteReader(buffer, encoding);
#if DEBUG
            for (; ; )
            {
                var read = converter.TryRead(ref reader, ref state, out result);
                if (read)
                {
                    state.BytesNeeded = 0;
                    return reader.Position;
                }
            }
#else
            var read = converter.TryRead(ref reader, ref state, out result);
            if (read)
                state.BytesNeeded = 0;
            return reader.Position;
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(ByteConverter converter, ReadOnlySpan<byte> buffer, ref ReadState state, Encoding encoding, out object? result)
        {
            var reader = new ByteReader(buffer, encoding);
#if DEBUG
            for (; ; )
            {
                var read = converter.TryReadBoxed(ref reader, ref state, out result);
                if (read)
                {
                    state.BytesNeeded = 0;
                    return reader.Position;
                }
            }
#else
            var read = converter.TryReadBoxed(ref reader, ref state, out result);
            if (read)
                state.BytesNeeded = 0;
            return reader.Position;
#endif
        }
    }
}