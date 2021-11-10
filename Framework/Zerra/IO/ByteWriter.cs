// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Buffers;
using System.Text;

namespace Zerra.IO
{
    public ref partial struct ByteWriter
    {
        private static readonly Encoding defaultEncoding = Encoding.Unicode;

        private const byte nullByte = 0;
        private const byte notNullByte = 1;

        private readonly bool fromPool;
        private byte[] bufferOwner;

        private Span<byte> buffer;

        private int position;
        private int length;

        private readonly Encoding encoding;

        public int Position => position;
        public int Length => length;

        public ByteWriter(Span<byte> buffer, Encoding encoding = null)
        {
            this.fromPool = false;
            this.bufferOwner = null;
            this.buffer = buffer;
            this.encoding = encoding ?? defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteWriter(int initialSize, Encoding encoding = null)
        {
            this.fromPool = true;
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

            if (!fromPool)
                throw new InvalidOperationException($"{nameof(ByteWriter)} has reached it's buffer limit");

            var minSize = position + additionalSize;

            BufferArrayPool<byte>.Grow(ref bufferOwner, minSize);
            buffer = bufferOwner;
            length = buffer.Length;
        }

        public byte[] ToArray()
        {
            var bytes = buffer.Slice(0, position).ToArray();
            return bytes;
        }

        public void Dispose()
        {
            if (fromPool)
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
}