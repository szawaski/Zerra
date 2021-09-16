// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.IO
{
    public ref partial struct CharReader
    {
        private const int initialStreamBufferSize = 16 * 1024;

        private readonly StreamReader stream;
        private char[] streamBufferOwner;
        private Span<char> streamBuffer;

        private ReadOnlySpan<char> buffer;

        private int bufferPosition;
        private int bufferLength;

        private int segmentStart;

        public CharReader(string chars)
        {
            this.stream = null;
            this.streamBufferOwner = null;
            this.streamBuffer = null;
            this.buffer = chars.AsSpan();
            this.bufferPosition = 0;
            this.bufferLength = this.buffer.Length;
            this.segmentStart = -1;
        }
        public CharReader(ReadOnlySpan<char> chars)
        {
            this.stream = null;
            this.streamBufferOwner = null;
            this.streamBuffer = null;
            this.buffer = chars;
            this.bufferPosition = 0;
            this.bufferLength = this.buffer.Length;
            this.segmentStart = -1;
        }
        public CharReader(Stream stream, Encoding encoding)
        {
            this.stream = new StreamReader(stream, encoding);
            this.streamBufferOwner = BufferArrayPool<char>.Rent(initialStreamBufferSize);
            this.streamBuffer = this.streamBufferOwner;
            this.buffer = this.streamBuffer;
            this.bufferPosition = 0;
            this.bufferLength = 0;
            this.segmentStart = -1;
        }

        public bool HasMoreChars()
        {
            if (bufferLength - bufferPosition > 0)
                return true;

            if (stream == null)
                return false;

            ReadBuffer(1);

            if (bufferLength - bufferPosition > 0)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ReadBuffer(int sizeNeeded)
        {
            if (stream == null)
                return;

            if (bufferPosition > 1)
            {
                //Always keep one char for BackOne or hold chars for segmentStart
                int shift;
                if (segmentStart > -1)
                    shift = segmentStart;
                else
                    shift = bufferPosition - 1;
                streamBuffer.Slice(shift, bufferLength - shift).CopyTo(streamBuffer);
                segmentStart -= shift;
                bufferLength -= shift;
                bufferPosition -= shift;
            }

            if (streamBuffer.Length - bufferPosition < sizeNeeded)
            {
                BufferArrayPool<char>.Grow(ref streamBufferOwner, bufferLength + sizeNeeded);
                streamBuffer = streamBufferOwner;
                buffer = streamBuffer;
            }

            while (bufferLength < streamBuffer.Length)
            {
#if NETSTANDARD2_0
                var read = stream.ReadToSpan(streamBuffer.Slice(bufferLength));
#else

                var read = stream.Read(streamBuffer.Slice(bufferLength));
#endif
                if (read == 0)
                    break;
                bufferLength += read;
            }
        }

        public void Dispose()
        {
            if (streamBufferOwner != null)
            {
                streamBuffer.Clear();
                BufferArrayPool<char>.Return(streamBufferOwner);
                streamBufferOwner = null;
                streamBuffer = null;
            }
            if (stream != null)
                stream.Dispose();
        }

        public FormatException CreateException(string message = null)
        {
            const int helperLength = 32;
            var start = bufferPosition > helperLength ? bufferPosition - helperLength - 2 : 0;
            var length = start + helperLength > bufferPosition ? bufferPosition - start : helperLength;
            var helper = buffer.Slice(start, length).ToString();
            return new FormatException($"{(message ?? "Error")} at position {bufferPosition} character after {helper}");
        }
    }
}