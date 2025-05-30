﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zerra.IO
{
    /// <summary>
    /// Wraps a stream so that each method can be overridded to intercept operations as needed.
    /// </summary>
    public abstract class StreamWrapper : Stream
    {
        private bool disposed = false;

        protected readonly Stream stream;
        protected readonly bool leaveOpen;

        public StreamWrapper(Stream stream, bool leaveOpen)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.leaveOpen = leaveOpen;
        }

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanWrite => stream.CanWrite;
        public override long Length => stream.Length;
        public override long Position { get => stream.Position; set => stream.Position = value; }
        public override bool CanTimeout => stream.CanTimeout;
        public override int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        public override int WriteTimeout { get => stream.WriteTimeout; set => stream.WriteTimeout = value; }

        public override void Flush() => stream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
        public override void SetLength(long value) => stream.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
        public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => stream.ReadAsync(buffer, offset, count, cancellationToken);
        public override int ReadByte() => stream.ReadByte();
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => stream.BeginRead(buffer, offset, count, callback, state);
        public override int EndRead(IAsyncResult asyncResult) => stream.EndRead(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => stream.WriteAsync(buffer, offset, count, cancellationToken);
        public override void WriteByte(byte value) => stream.WriteByte(value);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) => stream.BeginWrite(buffer, offset, count, callback, state);
        public override void EndWrite(IAsyncResult asyncResult) => stream.EndWrite(asyncResult);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => stream.CopyToAsync(destination, bufferSize, cancellationToken);

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
        public override int Read(Span<byte> buffer) => stream.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => stream.ReadAsync(buffer, cancellationToken);

        public override void Write(ReadOnlySpan<byte> buffer) => stream.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => stream.WriteAsync(buffer, cancellationToken);

        public override void CopyTo(Stream destination, int bufferSize) => stream.CopyTo(destination, bufferSize);

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