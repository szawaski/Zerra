// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamTransform"/> class.
        /// </summary>
        /// <param name="stream">The underlying stream to wrap.</param>
        /// <param name="leaveOpen">If true, the underlying stream will not be closed when this stream closes or disposes; otherwise false.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
        public StreamTransform(Stream stream, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Gets a value indicating whether the underlying stream supports reading.
        /// </summary>
        public override bool CanRead => stream.CanRead;
        /// <summary>
        /// Gets a value indicating whether the underlying stream supports seeking.
        /// </summary>
        public override bool CanSeek => stream.CanSeek;
        /// <summary>
        /// Gets a value indicating whether the underlying stream supports writing.
        /// </summary>
        public override bool CanWrite => stream.CanWrite;
        /// <summary>
        /// Gets the length of the stream in bytes. This is an abstract property that must be implemented by derived classes.
        /// </summary>
        public override abstract long Length { get; }
        /// <summary>
        /// Gets or sets the current position within the stream. This is an abstract property that must be implemented by derived classes.
        /// </summary>
        public override abstract long Position { get; set; }
        /// <inheritdoc/>
        public override sealed bool CanTimeout => stream.CanTimeout;
        /// <inheritdoc/>
        public override sealed int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        /// <inheritdoc/>
        public override sealed int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        /// <inheritdoc/>
        public override void Flush() => stream.Flush();
        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

        /// <summary>
        /// Sets the position within the stream. This is an abstract method that must be implemented by derived classes.
        /// </summary>
        /// <param name="offset">The byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the stream.</returns>
        public override abstract long Seek(long offset, SeekOrigin origin);
        /// <summary>
        /// Sets the length of the stream. This is an abstract method that must be implemented by derived classes.
        /// </summary>
        /// <param name="value">The desired length of the stream in bytes.</param>
        public override abstract void SetLength(long value);

        /// <inheritdoc/>
        public override sealed int ReadByte()
        {
            Span<byte> buffer = stackalloc byte[1];
            return InternalRead(buffer);
        }
        /// <inheritdoc/>
        public override sealed void WriteByte(byte value)
        {
            Span<byte> buffer = stackalloc byte[1];
            InternalWrite(buffer);
        }

        /// <inheritdoc/>
        public override sealed IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override sealed int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override sealed IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override sealed void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override sealed int Read(byte[] buffer, int offset, int count) => InternalRead(buffer.AsSpan().Slice(offset, count));
        /// <inheritdoc/>
        public override sealed void Write(byte[] buffer, int offset, int count) => InternalWrite(buffer.AsSpan().Slice(offset, count));

        /// <inheritdoc/>
        public override sealed Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => InternalReadAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask();
        /// <inheritdoc/>
        public override sealed Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => InternalWriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask();

#if NETSTANDARD2_0
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override sealed void Close()
        {
            base.Close(); //calls dispose underneath
        }

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override sealed int Read(Span<byte> buffer) => InternalRead(buffer);
        /// <inheritdoc/>
        public override sealed ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => InternalReadAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override sealed void Write(ReadOnlySpan<byte> buffer) => InternalWrite(buffer);
        /// <inheritdoc/>
        public override sealed ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => InternalWriteAsync(buffer, cancellationToken);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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