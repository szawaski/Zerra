// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Zerra.Buffers;

namespace Zerra.Compression
{
    //collects what's written in a buffer from the pool, MemoryStream grows by allocating
    internal sealed class PooledWriteStream : Stream
    {
        private byte[]? buffer;
        private int length;

        public PooledWriteStream(int capacity)
        {
            this.buffer = ArrayPoolHelper<byte>.Rent(capacity);
            this.length = 0;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => length;
        public override long Position { get => length; set => throw new NotSupportedException(); }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.buffer is null)
                throw new ObjectDisposedException(nameof(PooledWriteStream));
            if (length + count > this.buffer.Length)
                ArrayPoolHelper<byte>.Grow(ref this.buffer, length + count);
            Buffer.BlockCopy(buffer, offset, this.buffer, length, count);
            length += count;
        }

#if !NETSTANDARD2_0
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (this.buffer is null)
                throw new ObjectDisposedException(nameof(PooledWriteStream));
            if (length + buffer.Length > this.buffer.Length)
                ArrayPoolHelper<byte>.Grow(ref this.buffer, length + buffer.Length);
            buffer.CopyTo(this.buffer.AsSpan(length));
            length += buffer.Length;
        }
#endif

        public override void WriteByte(byte value)
        {
            if (buffer is null)
                throw new ObjectDisposedException(nameof(PooledWriteStream));
            if (length + 1 > buffer.Length)
                ArrayPoolHelper<byte>.Grow(ref buffer, length + 1);
            buffer[length++] = value;
        }

        public byte[] ToArray()
        {
            if (buffer is null)
                throw new ObjectDisposedException(nameof(PooledWriteStream));
            var bytes = new byte[length];
            Buffer.BlockCopy(buffer, 0, bytes, 0, length);
            return bytes;
        }

        protected override void Dispose(bool disposing)
        {
            if (buffer is not null)
            {
                ArrayPoolHelper<byte>.Return(buffer, length);
                buffer = null;
            }
            base.Dispose(disposing);
        }
    }
}
