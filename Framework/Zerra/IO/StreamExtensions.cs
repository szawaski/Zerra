// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license


// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Buffers;

namespace Zerra.IO
{
    /// <summary>
    /// Provides extension methods for <see cref="Stream"/>, <see cref="StreamReader"/>, and <see cref="StreamWriter"/> classes.
    /// </summary>
    public static class StreamExtensions
    {
        private const int bufferSize = 1024 * 16;

#if NETSTANDARD2_0
        /// <summary>
        /// Reads the entire contents of the stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A byte array containing all the data from the stream.</returns>
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

        /// <summary>
        /// Asynchronously reads the entire contents of the stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the byte array with all the data from the stream.</returns>
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
        /// <summary>
        /// Reads the entire contents of the stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>A byte array containing all the data from the stream.</returns>
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

        /// <summary>
        /// Asynchronously reads the entire contents of the stream into a byte array.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the byte array with all the data from the stream.</returns>
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
        /// <summary>
        /// Reads data from the stream into the specified span.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="span">The span to read the data into.</param>
        /// <returns>The number of bytes read from the stream.</returns>
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

        /// <summary>
        /// Asynchronously reads data from the stream into the specified memory region.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="memory">The memory region to read the data into.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the number of bytes read from the stream.</returns>
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

        /// <summary>
        /// Writes data from the specified span to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="span">The span containing the data to write.</param>
        public static void WriteToSpan(this Stream stream, Span<byte> span)
        {
            var buffer = span.ToArray();
            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Asynchronously writes data from the specified memory region to the stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="memory">The memory region containing the data to write.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static Task WriteToMemory(this Stream stream, Memory<byte> memory, CancellationToken cancellationToken = default)
        {
            var buffer = memory.ToArray();
            return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        }

        /// <summary>
        /// Reads data from the stream reader into the specified span.
        /// </summary>
        /// <param name="stream">The stream reader to read from.</param>
        /// <param name="span">The span to read the data into.</param>
        /// <returns>The number of characters read from the stream reader.</returns>
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

        /// <summary>
        /// Asynchronously reads data from the stream reader into the specified memory region.
        /// </summary>
        /// <param name="stream">The stream reader to read from.</param>
        /// <param name="memory">The memory region to read the data into.</param>
        /// <returns>A task that represents the asynchronous read operation. The task result contains the number of characters read from the stream reader.</returns>
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

        /// <summary>
        /// Writes data from the specified span to the stream writer.
        /// </summary>
        /// <param name="stream">The stream writer to write to.</param>
        /// <param name="span">The span containing the data to write.</param>
        public static void WriteToSpan(this StreamWriter stream, Span<char> span)
        {
            var temp = span.ToArray();
            stream.Write(temp, 0, temp.Length);
        }

        /// <summary>
        /// Asynchronously writes data from the specified memory region to the stream writer.
        /// </summary>
        /// <param name="stream">The stream writer to write to.</param>
        /// <param name="memory">The memory region containing the data to write.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static Task WriteToMemory(this StreamWriter stream, Memory<char> memory)
        {
            var temp = memory.ToArray();
            return stream.WriteAsync(temp, 0, temp.Length);
        }
#endif
    }
}

