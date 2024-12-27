// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Buffers;

namespace Zerra.IO
{
    /// <summary>
    /// This wraps a stream to intercept the reading and writing of bytes to perform operations on them before going to the underlying stream.
    /// </summary>
    public abstract class StreamTransform : Stream
    {
        private bool disposed = false;

        /// <summary>
        /// The underlying stream.
        /// </summary>
        protected readonly Stream stream;
        /// <summary>
        /// Indicates the underlying stream will not close when this stream closes or disposes if True.
        /// </summary>
        protected readonly bool leaveOpen;

        public StreamTransform(Stream stream, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
        }

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanWrite => stream.CanWrite;
        public override abstract long Length { get; }
        public override abstract long Position { get; set; }
        public override sealed bool CanTimeout => stream.CanTimeout;
        public override sealed int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        public override sealed int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        public override void Flush() => stream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

        public override abstract long Seek(long offset, SeekOrigin origin);
        public override abstract void SetLength(long value);

        public override sealed int ReadByte()
        {
            Span<byte> buffer = stackalloc byte[1];
            return InternalRead(buffer);
        }
        public override sealed void WriteByte(byte value)
        {
            Span<byte> buffer = stackalloc byte[1];
            InternalWrite(buffer);
        }

        public override sealed IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
        public override sealed int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();

        public override sealed IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
        public override sealed void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

        public override sealed int Read(byte[] buffer, int offset, int count) => InternalRead(buffer.AsSpan().Slice(offset, count));
        public override sealed void Write(byte[] buffer, int offset, int count) => InternalWrite(buffer.AsSpan().Slice(offset, count));

        public override sealed Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => InternalReadAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask();
        public override sealed Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => InternalWriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask();

#if NETSTANDARD2_0
        public override sealed async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var bufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
            var maxSize = 0;
            try
            {
                int read;
                while ((read = await InternalReadAsync(bufferOwner.AsMemory().Slice(0, bufferOwner.Length), cancellationToken)) > 0)
                {
                    await destination.WriteAsync(bufferOwner, 0, read, cancellationToken);
                    if (read > maxSize)
                        maxSize = read;
                }
            }
            finally
            {
                ArrayPoolHelper<byte>.Return(bufferOwner);
            }
        }
#else
        public override sealed async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var bufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
            var buffer = bufferOwner.AsMemory();
            var maxSize = 0;
            try
            {
                int read;
                while ((read = await InternalReadAsync(buffer, cancellationToken)) > 0)
                {
                    await destination.WriteAsync(buffer.Slice(0, read), cancellationToken);
                    if (read > maxSize)
                        maxSize = read;
                }
            }
            finally
            {
                buffer.Span.Slice(0, maxSize).Clear();
                ArrayPoolHelper<byte>.Return(bufferOwner);
            }
        }
#endif
        public override sealed void Close()
        {
            base.Close(); //calls dispose underneath
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                if (!leaveOpen)
                    stream.Dispose();
                base.Dispose(disposing);
            }
        }

#if !NETSTANDARD2_0
        public override sealed int Read(Span<byte> buffer) => InternalRead(buffer);
        public override sealed ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => InternalReadAsync(buffer, cancellationToken);

        public override sealed void Write(ReadOnlySpan<byte> buffer) => InternalWrite(buffer);
        public override sealed ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => InternalWriteAsync(buffer, cancellationToken);

        public override sealed void CopyTo(Stream destination, int bufferSize)
        {
            var bufferOwner = ArrayPoolHelper<byte>.Rent(bufferSize);
            var buffer = bufferOwner.AsSpan();
            var maxSize = 0;
            try
            {
                int read;
                while ((read = InternalRead(buffer)) > 0)
                {
                    destination.Write(buffer.Slice(0, read));
                    if (read > maxSize)
                        maxSize = read;
                }
            }
            finally
            {
                buffer.Slice(0, maxSize).Clear();
                ArrayPoolHelper<byte>.Return(bufferOwner);
            }
        }

        public override async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                disposed = true;
                if (!leaveOpen)
                    await stream.DisposeAsync();
                await base.DisposeAsync();
                GC.SuppressFinalize(this);
            }
        }
#endif

        /// <summary>
        /// Perform the syncronous read transformation of the underlying stream.
        /// The implementation should be using the protected <see cref="stream"/> field.
        /// </summary>
        /// <param name="buffer">The incomming buffer to read.</param>
        /// <returns>The number of bytes read.</returns>
        protected abstract int InternalRead(Span<byte> buffer);
        /// <summary>
        /// Perform the asyncronous read transformation of the underlying stream.
        /// The implementation should be using the protected <see cref="stream"/> field.
        /// </summary>
        /// <param name="buffer">The incomming buffer to read.</param>\
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The number of bytes read.</returns>
        protected abstract ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        /// <summary>
        /// Perform the syncronous write transformation of the underlying stream.
        /// The implementation should be using the protected <see cref="stream"/> field.
        /// </summary>
        /// <param name="buffer">The outgoing buffer to write.</param>
        protected abstract void InternalWrite(ReadOnlySpan<byte> buffer);
        /// <summary>
        /// Perform the asyncronous write transformation of the underlying stream.
        /// The implementation should be using the protected <see cref="stream"/> field.
        /// </summary>
        /// <param name="buffer">The outgoing buffer to write.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    }
}