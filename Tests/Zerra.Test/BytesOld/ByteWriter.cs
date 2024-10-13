// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteWriterOld
    {
        private const int defaultBufferSize = 1024;
        private static readonly Encoding defaultEncoding = Encoding.UTF8;

        private const byte nullByte = 0;
        private const byte notNullByte = 1;

        private byte[]? bufferOwner;
        private Span<byte> buffer;

        private int position;
        private int length;

        private readonly Encoding encoding;

        public readonly int Length => position;

        public ByteWriterOld()
        {
            this.bufferOwner = ArrayPoolHelper<byte>.Rent(defaultBufferSize);
            this.buffer = bufferOwner;
            this.encoding = defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteWriterOld(Span<byte> buffer, Encoding? encoding = null)
        {
            this.bufferOwner = null;
            this.buffer = buffer;
            this.encoding = encoding ?? defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteWriterOld(int initialSize, Encoding? encoding = null)
        {
            this.bufferOwner = ArrayPoolHelper<byte>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.encoding = encoding ?? defaultEncoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSize(int additionalSize)
        {
            if (position + additionalSize <= buffer.Length)
                return;

            if (bufferOwner is null)
                throw new InvalidOperationException($"{nameof(ByteWriter)} has reached it's buffer limit");

            var minSize = position + additionalSize;

            ArrayPoolHelper<byte>.Grow(ref bufferOwner, minSize);
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
            if (bufferOwner is not null)
            {
                buffer.Clear();
                ArrayPoolHelper<byte>.Return(bufferOwner);
                bufferOwner = null;
                buffer = null;
            }
        }
    }
}