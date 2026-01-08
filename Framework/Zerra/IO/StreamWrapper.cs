// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.IO
{
    /// <summary>
    /// Wraps a stream so that each method can be overridden to intercept operations as needed.
    /// </summary>
    public abstract class StreamWrapper : Stream
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
        /// Initializes a new instance of the <see cref="StreamWrapper"/> class.
        /// </summary>
        /// <param name="stream">The underlying stream to wrap.</param>
        /// <param name="leaveOpen">If true, the underlying stream will not be closed when this stream closes or disposes; otherwise false.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
        public StreamWrapper(Stream stream, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
        }

        /// <inheritdoc/>
        public override bool CanRead => stream.CanRead;
        /// <inheritdoc/>
        public override bool CanSeek => stream.CanSeek;
        /// <inheritdoc/>
        public override bool CanWrite => stream.CanWrite;
        /// <inheritdoc/>
        public override long Length => stream.Length;
        /// <inheritdoc/>
        public override long Position { get => stream.Position; set => stream.Position = value; }
        /// <inheritdoc/>
        public override bool CanTimeout => stream.CanTimeout;
        /// <inheritdoc/>
        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        /// <inheritdoc/>
        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        /// <inheritdoc/>
        public override void Flush() => stream.Flush();
        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
        /// <inheritdoc/>
        public override void SetLength(long value) => stream.SetLength(value);

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => stream.ReadAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc/>
        public override int ReadByte() => stream.ReadByte();
        /// <inheritdoc/>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => stream.BeginRead(buffer, offset, count, callback, state);
        /// <inheritdoc/>
        public override int EndRead(IAsyncResult asyncResult) => stream.EndRead(asyncResult);

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => stream.WriteAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc/>
        public override void WriteByte(byte value) => stream.WriteByte(value);
        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => stream.BeginWrite(buffer, offset, count, callback, state);
        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult) => stream.EndWrite(asyncResult);

        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => stream.CopyToAsync(destination, bufferSize, cancellationToken);

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
        public override int Read(Span<byte> buffer) => stream.Read(buffer);
        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => stream.ReadAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => stream.Write(buffer);
        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => stream.WriteAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override void CopyTo(Stream destination, int bufferSize) => stream.CopyTo(destination, bufferSize);

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
    }
}