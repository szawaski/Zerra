// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public ref struct ByteBuilder
    {
        private const int defaultBufferSize = 1024;

        private const byte nullByte = 0;
        private const byte notNullByte = 1;

        private byte[]? bufferOwner;
        private Span<byte> buffer;

        private int position;
        private int length;

        public readonly int Length => position;

        public ByteBuilder()
        {
            this.bufferOwner = BufferArrayPool<byte>.Rent(defaultBufferSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteBuilder(Span<byte> buffer)
        {
            this.bufferOwner = null;
            this.buffer = buffer;
            this.position = 0;
            this.length = buffer.Length;
        }

        public ByteBuilder(byte[] buffer, bool fromPool, int position = 0)
        {
            this.bufferOwner = fromPool ? buffer : null;
            this.buffer = buffer;
            this.position = position;
            this.length = buffer.Length;
        }

        public ByteBuilder(int initialSize)
        {
            this.bufferOwner = BufferArrayPool<byte>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSize(int additionalSize)
        {
            if (position + additionalSize <= buffer.Length)
                return;

            if (bufferOwner == null)
                throw new InvalidOperationException($"{nameof(ByteBuilder)} has reached it's buffer limit");

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(byte[] bytes)
        {
            if (bytes.Length == 0)
                return;
            EnsureBufferSize(bytes.Length);
            fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    pBuffer[i] = pBytes[i];
                }
            }
            position += bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            EnsureBufferSize(1);
            buffer[position++] = value;
        }
    }
}