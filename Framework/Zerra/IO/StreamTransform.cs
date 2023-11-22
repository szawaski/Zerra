// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.IO
{
    public abstract class StreamTransform : Stream
    {
        private bool disposed = false;

        protected readonly Stream stream;
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

        public override void Flush() { stream.Flush(); }
        public override Task FlushAsync(CancellationToken cancellationToken) { return stream.FlushAsync(cancellationToken); }

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

        public override sealed IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { throw new NotSupportedException(); }
        public override sealed int EndRead(IAsyncResult asyncResult) { throw new NotSupportedException(); }

        public override sealed IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) { throw new NotSupportedException(); }
        public override sealed void EndWrite(IAsyncResult asyncResult) { throw new NotSupportedException(); }

        public override sealed int Read(byte[] buffer, int offset, int count) { return InternalRead(buffer.AsSpan().Slice(offset, count)); }
        public override sealed void Write(byte[] buffer, int offset, int count) { InternalWrite(buffer.AsSpan().Slice(offset, count)); }

        public override sealed Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return InternalReadAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask(); }
        public override sealed Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) { return InternalWriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).AsTask(); }

#if NETSTANDARD2_0
        public override sealed async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var bufferOwner = BufferArrayPool<byte>.Rent(bufferSize);
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
                Array.Clear(bufferOwner, 0, maxSize);
                BufferArrayPool<byte>.Return(bufferOwner);
            }
        }
#else
        public override sealed async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            var bufferOwner = BufferArrayPool<byte>.Rent(bufferSize);
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
                BufferArrayPool<byte>.Return(bufferOwner);
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
        public override sealed int Read(Span<byte> buffer) { return InternalRead(buffer); }
        public override sealed ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { return InternalReadAsync(buffer, cancellationToken); }

        public override sealed void Write(ReadOnlySpan<byte> buffer) { InternalWrite(buffer); }
        public override sealed ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) { return InternalWriteAsync(buffer, cancellationToken); }

        public override sealed void CopyTo(Stream destination, int bufferSize)
        {
            var bufferOwner = BufferArrayPool<byte>.Rent(bufferSize);
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
                BufferArrayPool<byte>.Return(bufferOwner);
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

        protected abstract int InternalRead(Span<byte> buffer);
        protected abstract ValueTask<int> InternalReadAsync(Memory<byte> buffer, CancellationToken cancellationToken);

        protected abstract void InternalWrite(ReadOnlySpan<byte> buffer);
        protected abstract ValueTask InternalWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    }
}