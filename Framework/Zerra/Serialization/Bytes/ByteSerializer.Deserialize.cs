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

namespace Zerra.Serialization
{
    public static partial class ByteSerializer
    {
        public static T? Deserialize<T>(ReadOnlySpan<byte> bytes, ByteSerializerOptions? options = null)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var state = new ReadState()
            {
                Stack = new(),
                IncludePropertyTypes = options.IncludePropertyTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
            state.PushFrame(converter, true, null);

            Read(bytes, ref state, options.Encoding);

            if (!state.Ended || state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return (T?)state.Result;
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
                Stack = new(),
                IncludePropertyTypes = options.IncludePropertyTypes,
                UsePropertyNames = options.UsePropertyNames,
                IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
            };
            state.PushFrame(converter, true, null);

            Read(bytes, ref state, options.Encoding);

            if (!state.Ended || state.BytesNeeded > 0)
                throw new EndOfStreamException();

            return state.Result;
        }

        public static T? Deserialize<T>(Stream stream, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            options ??= defaultOptions;

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var isFinalBlock = false;
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

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
                    Stack = new(),
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
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
                }

                return (T?)state.Result;
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
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

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
                    Stack = new(),
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
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
                }

                return state.Result;
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
            var converter = ByteConverterFactory<object>.GetRoot(typeDetail);

            var isFinalBlock = false;
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

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
                    Stack = new(),
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Read(buffer.AsSpan().Slice(0, length), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
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
                }

                return (T?)state.Result;
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
#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

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
                    Stack = new(),
                    IncludePropertyTypes = options.IncludePropertyTypes,
                    UsePropertyNames = options.UsePropertyNames,
                    IgnoreIndexAttribute = options.IgnoreIndexAttribute,
                    IndexSizeUInt16 = options.IndexSize == ByteSerializerIndexSize.UInt16
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var bytesUsed = Read(buffer.AsSpan().Slice(0, read), ref state, options.Encoding);

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
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
                }

                return state.Result;
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Read(ReadOnlySpan<byte> buffer, ref ReadState state, Encoding encoding)
        {
            var reader = new ByteReader(buffer, encoding);
            for (; ; )
            {
                var read = state.Current.Converter.TryRead(ref reader, ref state, state.Current.Parent);
                //read = false && state.BytesNeeded == 0 indicates we needed to unwind the stack
                if (read || state.BytesNeeded > 0)
                    return reader.Position;
            }
        }
    }
}