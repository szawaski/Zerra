// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.IO
{
    public ref partial struct CharWriteBuffer
    {
        private const int defaultInitialSize = 16 * 1024;

        private readonly StreamWriter stream;
        private char[] bufferOwner;
        private Span<char> buffer;

        private int position;
        private int streamWritten;

        public CharWriteBuffer(int initialSize)
        {
            this.stream = null;
            this.bufferOwner = BufferArrayPool<char>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.streamWritten = 0;
        }

        public CharWriteBuffer(Stream stream, Encoding encoding, int initialSize = defaultInitialSize)
        {
            this.stream = new StreamWriter(stream, encoding);
            this.bufferOwner = BufferArrayPool<char>.Rent(initialSize);
            this.buffer = bufferOwner;
            this.position = 0;
            this.streamWritten = 0;
        }

        public int Length => position + streamWritten;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSize(int additionalLength)
        {
            if (position + additionalLength < buffer.Length)
                return;

            if (bufferOwner == null)
            {
                bufferOwner = BufferArrayPool<char>.Rent(defaultInitialSize);
                buffer = bufferOwner;
                if (position + additionalLength < buffer.Length)
                    return;
            }

            if (position > 0 && stream != null)
            {
#if NETSTANDARD2_0
                stream.WriteToSpan(buffer.Slice(0, position));
#else
                stream.Write(buffer.Slice(0, position));
#endif
                streamWritten += position;
                position = 0;

                if (position + additionalLength < buffer.Length)
                    return;
            }

            var neededLength = position + additionalLength;
            BufferArrayPool<char>.Grow(ref bufferOwner, neededLength);
            buffer = bufferOwner;
        }

        public void Clear()
        {
            if (streamWritten > 0)
                throw new InvalidOperationException($"cannot clear {nameof(CharWriteBuffer)} with data already written to the stream");
            position = 0;
        }

        public void Flush()
        {
            if (position == 0)
                return;
#if NETSTANDARD2_0
            stream.WriteToSpan(buffer.Slice(0, position));
#else
            stream.Write(buffer.Slice(0, position));
#endif
            streamWritten += position;
            position = 0;
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
            if (stream != null)
                stream.Dispose();
        }
    }
}