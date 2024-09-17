// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Zerra.Buffers;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.Converters;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes
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
                UseTypes = options.UseTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
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
                UseTypes = options.UseTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
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

                var state = new ReadState()
                {
                    UseTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                T? result;

                for (; ; )
                {
                    var bytesUsed = Read(converter, buffer.AsSpan().Slice(0, length), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    length -= bytesUsed;

                    if (length + state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.BytesNeeded);

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

                    if (length < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
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

                var state = new ReadState()
                {
                    UseTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    length -= bytesUsed;

                    if (length + state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.BytesNeeded);

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

                    if (length < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
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
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, length, buffer.Length - length);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(length));
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

                var state = new ReadState()
                {
                    UseTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                T? result;

                for (; ; )
                {
                    var usedBytes = Read(converter, buffer.AsSpan().Slice(0, length), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, usedBytes, buffer, 0, length - usedBytes);
                    length -= usedBytes;

                    if (length + state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.BytesNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, length, buffer.Length - length);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(length));
#endif

                        if (read == 0)
                        {
                            isFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
                }

                return result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                ArrayPoolHelper<byte>.Return(buffer);
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
            var buffer = ArrayPoolHelper<byte>.Rent(defaultBufferSize);

            try
            {
                var length = 0;
                var read = -1;
                while (length < buffer.Length)
                {
#if NETSTANDARD2_0
                    read = await stream.ReadAsync(buffer, length, buffer.Length - length);
#else
                    read = await stream.ReadAsync(buffer.AsMemory(length));
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

                var state = new ReadState()
                {
                    UseTypes = options.UseTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                object? result;

                for (; ; )
                {
                    var bytesUsed = ReadBoxed(converter, buffer.AsSpan().Slice(0, length), ref state, options.Encoding, out result);

                    if (state.BytesNeeded == 0)
                    {
                        if (!isFinalBlock)
                            throw new EndOfStreamException();
                        break;
                    }

                    if (isFinalBlock)
                        throw new EndOfStreamException();

                    Buffer.BlockCopy(buffer, bytesUsed, buffer, 0, length - bytesUsed);
                    length -= bytesUsed;

                    if (length + state.BytesNeeded > buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, length + state.BytesNeeded);

                    while (length < buffer.Length)
                    {
#if NETSTANDARD2_0
                        read = await stream.ReadAsync(buffer, length, buffer.Length - length);
#else
                        read = await stream.ReadAsync(buffer.AsMemory(length));
#endif

                        if (read == 0)
                        {
                            isFinalBlock = true;
                            break;
                        }
                        length += read;
                    }

                    if (length < state.BytesNeeded)
                        throw new EndOfStreamException();

                    state.BytesNeeded = 0;
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
        private static int Read<T>(ByteConverter<object, T> converter, ReadOnlySpan<byte> buffer, ref ReadState state, Encoding encoding, out T? result)
        {
            var reader = new ByteReader(buffer, encoding);
#if DEBUG
        again:
#endif
            var read = converter.TryRead(ref reader, ref state, out result);
            if (read)
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
            if (!read && ByteReader.Testing && reader.Position + state.BytesNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadBoxed(ByteConverter converter, ReadOnlySpan<byte> buffer, ref ReadState state, Encoding encoding, out object? result)
        {
            var reader = new ByteReader(buffer, encoding);
#if DEBUG
        again:
#endif
            var read = converter.TryReadBoxed(ref reader, ref state, out result);
            if (read)
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
            if (!read && ByteReader.Testing && reader.Position + state.BytesNeeded <= reader.Length)
                goto again;
#endif
            return reader.Position;
        }
    }
}