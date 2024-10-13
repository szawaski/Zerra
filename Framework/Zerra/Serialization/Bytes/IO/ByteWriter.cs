// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteWriter
    {
        private const int defaultBufferSize = 1024;

        private const byte nullByte = 0;
        private const byte notNullByte = 1;

        private byte[]? bufferOwner;
        private Span<byte> buffer;

        private int position;
        private int length;

        private readonly Encoding encoding;

        public readonly int Position => position;
        public readonly int Length => length;

        public ByteWriter()
        {
            throw new NotSupportedException($"{nameof(ByteWriter)} cannot use default constructor");
        }

        public ByteWriter(Span<byte> buffer, Encoding encoding)
        {
            this.bufferOwner = null;
            this.buffer = buffer;
            this.encoding = encoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteWriter(int initialSize, Encoding encoding)
        {
            this.bufferOwner = ArrayPoolHelper<byte>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.encoding = encoding;
            this.position = 0;
            this.length = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureSize(int sizeNeeded)
        {
            if (length - position >= sizeNeeded)
                return true;

            if (bufferOwner is null)
                return false;

            ArrayPoolHelper<byte>.Grow(ref bufferOwner, Math.Max(bufferOwner.Length * 2, bufferOwner.Length + sizeNeeded));
            buffer = bufferOwner;
            length = buffer.Length;

            return true;
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
                Array.Clear(bufferOwner, 0, position);
                ArrayPoolHelper<byte>.Return(bufferOwner);
                bufferOwner = null;
                buffer = null;
            }
        }
    }
}