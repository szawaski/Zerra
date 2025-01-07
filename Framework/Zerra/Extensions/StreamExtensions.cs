// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Buffers;

namespace Zerra.Extensions
{
    public static class StreamExtensions
    {
        private const int bufferSize = 1024 * 16;

#if NETSTANDARD2_0
        public static byte[] ToArray(this Stream stream)
        {
            var buffer = ArrayPoolHelper<byte>.Rent(bufferSize);
            var totalRead = 0;
            int read;
            try
            {
                while ((read = stream.Read(buffer, totalRead, buffer.Length - totalRead)) > 0)
                {
                    totalRead += read;
                    if (totalRead == buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, buffer.Length * 2);
                }

                var bytes = new byte[totalRead];
                Buffer.BlockCopy(buffer, 0, bytes, 0, totalRead);
                return bytes;
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }

        public static async Task<byte[]> ToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPoolHelper<byte>.Rent(bufferSize);
            var totalRead = 0;
            int read;
            try
            {
                while ((read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead, cancellationToken)) > 0)
                {
                    totalRead += read;
                    if (totalRead == buffer.Length)
                        ArrayPoolHelper<byte>.Grow(ref buffer, buffer.Length * 2);
                }

                var bytes = new byte[totalRead];
                Buffer.BlockCopy(buffer, 0, bytes, 0, totalRead);
                return bytes;
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
        }
#else
        public static byte[] ToArray(this Stream stream)
        {
            var bufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
            var buffer = bufferOwner.AsSpan();
            var totalRead = 0;
            int read;
            try
            {
                while ((read = stream.Read(buffer.Slice(totalRead, buffer.Length - totalRead))) > 0)
                {
                    totalRead += read;
                    if (totalRead == buffer.Length)
                    {
                        ArrayPoolHelper<byte>.Grow(ref bufferOwner, bufferOwner.Length * 2);
                        buffer = bufferOwner.AsSpan();
                    }
                }

                var bytes = new byte[totalRead];
                Buffer.BlockCopy(bufferOwner, 0, bytes, 0, totalRead);
                return bytes;
            }
            finally
            {
                buffer.Slice(0, totalRead).Clear();
                ArrayPoolHelper<byte>.Return(bufferOwner);
            }
        }

        public static async Task<byte[]> ToArrayAsync(this Stream stream, CancellationToken cancellationToken = default)
        {
            var bufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
            var buffer = bufferOwner.AsMemory();
            var totalRead = 0;
            int read;
            try
            {
                while ((read = await stream.ReadAsync(buffer.Slice(totalRead, buffer.Length - totalRead), cancellationToken)) > 0)
                {
                    totalRead += read;
                    if (totalRead == buffer.Length)
                    {
                        ArrayPoolHelper<byte>.Grow(ref bufferOwner, buffer.Length * 2);
                        buffer = bufferOwner.AsMemory();
                    }
                }

                var bytes = new byte[totalRead];
                buffer.Slice(0, totalRead).Span.CopyTo(bytes.AsSpan());
                return bytes;
            }
            finally
            {
                buffer.Span.Slice(0, totalRead).Clear();
                ArrayPoolHelper<byte>.Return(bufferOwner);
            }
        }
#endif

#if NETSTANDARD2_0
        public static int ReadToSpan(this Stream stream, Span<byte> span)
        {
            var buffer = ArrayPoolHelper<byte>.Rent(span.Length);
            var totalRead = 0;
            try
            {
                totalRead = stream.Read(buffer, 0, span.Length);
                buffer.AsSpan().Slice(0, totalRead).CopyTo(span);
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
            return totalRead;
        }

        public static async Task<int> ReadToMemoryAsync(this Stream stream, Memory<byte> memory, CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPoolHelper<byte>.Rent(memory.Length);
            var totalRead = 0;
            try
            {
                totalRead = await stream.ReadAsync(buffer, 0, memory.Length, cancellationToken);
                buffer.AsSpan().Slice(0, totalRead).CopyTo(memory.Span);
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(buffer);
            }
            return totalRead;
        }

        public static void WriteToSpan(this Stream stream, Span<byte> span)
        {
            var buffer = span.ToArray();
            stream.Write(buffer, 0, buffer.Length);
        }

        public static Task WriteToMemory(this Stream stream, Memory<byte> memory, CancellationToken cancellationToken = default)
        {
            var buffer = memory.ToArray();
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        public static int ReadToSpan(this StreamReader stream, Span<char> span)
        {
            var buffer = ArrayPoolHelper<char>.Rent(span.Length);
            var totalRead = 0;
            try
            {
                totalRead = stream.Read(buffer, 0, span.Length);
                buffer.AsSpan().Slice(0, totalRead).CopyTo(span);
            }
            finally
            {
                ArrayPoolHelper<char>.Return(buffer);
            }
            return totalRead;
        }

        public static async Task<int> ReadToMemoryAsync(this StreamReader stream, Memory<char> memory)
        {
            var buffer = ArrayPoolHelper<char>.Rent(memory.Length);
            var totalRead = 0;
            try
            {
                totalRead = await stream.ReadAsync(buffer, 0, memory.Length);
                buffer.AsSpan().Slice(0, totalRead).CopyTo(memory.Span);
            }
            finally
            {
                ArrayPoolHelper<char>.Return(buffer);
            }
            return totalRead;
        }

        public static void WriteToSpan(this StreamWriter stream, Span<char> span)
        {
            var temp = span.ToArray();
            stream.Write(temp, 0, temp.Length);
        }

        public static Task WriteToMemory(this StreamWriter stream, Memory<char> memory)
        {
            var temp = memory.ToArray();
            return stream.WriteAsync(temp, 0, temp.Length);
        }
#endif
    }
}

