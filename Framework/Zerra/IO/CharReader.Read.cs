// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public ref partial struct CharReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Read(out char c)
        {
            if (bufferLength - bufferPosition < 1) ReadBuffer(1);
            if (bufferPosition >= bufferLength)
            {
                c = default;
                return false;
            }
            c = buffer[bufferPosition++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadSkipWhiteSpace(out char c)
        {
            if (bufferLength - bufferPosition < 1) ReadBuffer(1);
            if (bufferPosition >= bufferLength)
            {
                c = default;
                return false;
            }
            c = buffer[bufferPosition++];
            while (c == ' ' || c == '\r' || c == '\n' || c == '\t')
            {
                if (bufferLength - bufferPosition < 1) ReadBuffer(1);
                if (bufferPosition >= bufferLength)
                    return false;
                c = buffer[bufferPosition++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadUntil(out char c, char value)
        {
            if (bufferLength - bufferPosition < 1) ReadBuffer(1);
            if (bufferPosition >= bufferLength)
            {
                c = default;
                return false;
            }
            c = buffer[bufferPosition++];
            while (c != value)
            {
                if (bufferLength - bufferPosition < 1) ReadBuffer(1);
                if (bufferPosition >= bufferLength)
                    return false;
                c = buffer[bufferPosition++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadUntil(out char c, char value1, char value2)
        {
            if (bufferLength - bufferPosition < 1) ReadBuffer(1);
            if (bufferPosition >= bufferLength)
            {
                c = default;
                return false;
            }
            c = buffer[bufferPosition++];
            while (c != value1 && c != value2)
            {
                if (bufferLength - bufferPosition < 1) ReadBuffer(1);
                if (bufferPosition >= bufferLength)
                    return false;
                c = buffer[bufferPosition++];
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Foward(int count)
        {
            if (bufferLength - bufferPosition < count) ReadBuffer(count);
            if (bufferPosition + count >= bufferLength)
                return false;
            bufferPosition += count;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BackOne()
        {
            bufferPosition--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginSegment(bool includeCurrent)
        {
            segmentStart = bufferPosition - (includeCurrent ? 1 : 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> EndSegmentToSpan(bool includeCurrent)
        {
            var segment = buffer.Slice(segmentStart, bufferPosition - segmentStart - (includeCurrent ? 0 : 1));
            segmentStart = -1;
            return segment;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EndSegmentToString(bool includeCurrent)
        {
            if (segmentStart == -1)
                return null;
            var segment = buffer.Slice(segmentStart, bufferPosition - segmentStart - (includeCurrent ? 0 : 1)).ToString();
            segmentStart = -1;
            return segment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndSegmentCopyTo(bool includeCurrent, ref CharWriteBuffer writer)
        {
            writer.Write(buffer.Slice(segmentStart, bufferPosition - segmentStart - (includeCurrent ? 0 : 1)));
            segmentStart = -1;
        }
    }
}