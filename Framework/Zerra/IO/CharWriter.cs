// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public ref partial struct CharWriter
    {
        private const int defaultBufferSize = 1024;

        private char[] bufferOwner;
        private Span<char> buffer;

        private int position;
        private int length;

        public CharWriter()
        {
            this.bufferOwner = BufferArrayPool<char>.Rent(defaultBufferSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        public CharWriter(int initialSize)
        {
            this.bufferOwner = BufferArrayPool<char>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.length = buffer.Length;
        }

        public CharWriter(Span<char> buffer)
        {
            this.bufferOwner = null;
            this.buffer = buffer;
            this.position = 0;
            this.length = buffer.Length;
        }

        public CharWriter(char[] buffer, bool fromPool, int position = 0)
        {
            this.bufferOwner = fromPool ? buffer : null;
            this.buffer = buffer;
            this.position = position;
            this.length = buffer.Length;
        }

        public int Position => position;
        public int Length => length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSize(int additionalSize)
        {
            if (position + additionalSize <= buffer.Length)
                return;

            if (bufferOwner == null)
                throw new InvalidOperationException($"{nameof(CharWriter)} has reached it's buffer limit");

            var minSize = position + additionalSize;
            BufferArrayPool<char>.Grow(ref bufferOwner, minSize);
            buffer = bufferOwner;
        }

        public void Clear()
        {
            position = 0;
        }

        public Span<char> ToSpan()
        {
            return buffer.Slice(0, position);
        }
        public char[] ToArray()
        {
            return buffer.Slice(0, position).ToArray();
        }
        public override string ToString()
        {
            return buffer.Slice(0, position).ToString();
        }

        public void Dispose()
        {
            if (bufferOwner != null)
            {
                buffer.Clear();
                BufferArrayPool<char>.Return(bufferOwner);
                bufferOwner = null;
                buffer = null;
            }
        }
    }
}