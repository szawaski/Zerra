// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;

namespace Zerra.IO
{
    public ref partial struct ByteWriter
    {
        private const int defaultBufferSize = 1024;
        private static readonly Encoding defaultEncoding = Encoding.Unicode;

        private const byte nullByte = 0;
        private const byte notNullByte = 1;

        private byte[]? bufferOwner;
        private Span<byte> buffer;

        private int position;
        private int length;

        private readonly Encoding encoding;

        public readonly int Length => position;

        public ByteWriter()
        {
            this.bufferOwner = BufferArrayPool<byte>.Rent(defaultBufferSize);
            this.buffer = bufferOwner;
            this.encoding = defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteWriter(Span<byte> buffer, Encoding? encoding = null)
        {
            this.bufferOwner = null;
            this.buffer = buffer;
            this.encoding = encoding ?? defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteWriter(byte[] buffer, bool fromPool, int position = 0, Encoding? encoding = null)
        {
            this.bufferOwner = fromPool ? buffer : null;
            this.buffer = buffer;
            this.encoding = encoding ?? defaultEncoding;
            this.position = position;
            this.length = buffer.Length;
        }

        public ByteWriter(int initialSize, Encoding? encoding = null)
        {
            this.bufferOwner = BufferArrayPool<byte>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.encoding = encoding ?? defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        private void EnsureBufferSize(int additionalSize)
        {
            if (position + additionalSize <= buffer.Length)
                return;

            if (bufferOwner == null)
                throw new InvalidOperationException($"{nameof(ByteWriter)} has reached it's buffer limit");

            var minSize = position + additionalSize;

            BufferArrayPool<byte>.Grow(ref bufferOwner, minSize);
            buffer = bufferOwner;
            length = buffer.Length;
        }

        public void Clear()
        {
            position = 0;
        }

        public readonly Span<byte> ToSpan()
        {
            return buffer.Slice(0, position);
        }
        public readonly byte[] ToArray()
        {
            return buffer.Slice(0, position).ToArray();
        }

        public void Dispose()
        {
            if (bufferOwner != null)
            {
                buffer.Clear();
                BufferArrayPool<byte>.Return(bufferOwner);
                bufferOwner = null;
                buffer = null;
            }
        }
    }
}