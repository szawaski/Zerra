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
        public static byte[] Serialize<T>(T? obj, ByteSerializerOptions? options = null)
        {
            if (obj == null)
                return Array.Empty<byte>();

            options ??= defaultOptions;
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var position = 0;

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer.AsSpan().Slice(position), ref state, options.Encoding);

                    position += usedBytes;

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length - position)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded + position);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                BufferArrayPool<byte>.Return(buffer);
            }

            var result = new byte[position];
            Buffer.BlockCopy(buffer, 0, result, 0, position);
            return result;
        }
        public static byte[] Serialize(object? obj, ByteSerializerOptions? options = null)
        {
            if (obj == null)
                return Array.Empty<byte>();

            options ??= defaultOptions;
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var position = 0;

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer.AsSpan().Slice(position), ref state, options.Encoding);

                    position += usedBytes;

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length - position)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded + position);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                BufferArrayPool<byte>.Return(buffer);
            }

            var result = new byte[position];
            Buffer.BlockCopy(buffer, 0, result, 0, position);
            return result;
        }
        public static byte[] Serialize(object? obj, Type type, ByteSerializerOptions? options = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (obj == null)
                return Array.Empty<byte>();

            options ??= defaultOptions;
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif
            var position = 0;

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer.AsSpan().Slice(position), ref state, options.Encoding);

                    position += usedBytes;

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length - position)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded + position);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                BufferArrayPool<byte>.Return(buffer);
            }

            var result = new byte[position];
            Buffer.BlockCopy(buffer, 0, result, 0, position);
            return result;
        }

        public static void Serialize<T>(Stream stream, T? obj, ByteSerializerOptions? options = null)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (obj == null)
                return;

            options ??= defaultOptions;
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
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
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
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
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    stream.Write(buffer, 0, usedBytes);
#else
                    stream.Write(buffer.AsSpan(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
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
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = TypeAnalyzer<T>.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
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
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = obj.GetType().GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
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
            var byteConverterOptions = GetByteConverterOptions(options);

            var typeDetail = type.GetTypeDetail();
            var converter = ByteConverterFactory<object>.GetRoot(byteConverterOptions, typeDetail);

#if DEBUG
            var buffer = BufferArrayPool<byte>.Rent(Testing ? 1 : defaultBufferSize);
#else
            var buffer = BufferArrayPool<byte>.Rent(defaultBufferSize);
#endif

            try
            {
                var state = new WriteState()
                {
                    Stack = new(),
                    Object = obj
                };
                state.PushFrame(converter, true, null);

                for (; ; )
                {
                    var usedBytes = Write(buffer, ref state, options.Encoding);

#if NETSTANDARD2_0
                    await stream.WriteAsync(buffer, 0, usedBytes);
#else
                    await stream.WriteAsync(buffer.AsMemory(0, usedBytes));
#endif

                    if (state.Ended)
                        break;

                    if (state.BytesNeeded > 0)
                    {
                        if (state.BytesNeeded > buffer.Length)
                            BufferArrayPool<byte>.Grow(ref buffer, state.BytesNeeded);

                        state.BytesNeeded = 0;
                    }
                }
            }
            finally
            {
                Array.Clear(buffer, 0, buffer.Length);
                BufferArrayPool<byte>.Return(buffer);
            }
        }        

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Write(Span<byte> buffer, ref WriteState state, Encoding encoding)
        {
            var writer = new ByteWriter(buffer, encoding);
            for (; ; )
            {
                var write = state.Current.Converter.TryWrite(ref writer, ref state, state.Current.Parent);
                //write = false && state.BytesNeeded == 0 indicates we needed to unwind the stack
                if (write || state.BytesNeeded > 0)
                    return writer.Length;
            }
        }
    }
}