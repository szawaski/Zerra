// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public ref partial struct CharReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out char c)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSkipWhiteSpace(out char c)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            while (c == ' ' || c == '\r' || c == '\n' || c == '\t')
            {
                if (position >= length)
                    return false;
                c = buffer[position++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUntil(out char c, char value)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            while (c != value)
            {
                if (position >= length)
                    return false;
                c = buffer[position++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUntil(out char c, char value1, char value2)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            while (c != value1 && c != value2)
            {
                if (position >= length)
                    return false;
                c = buffer[position++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUntil(out char c, char value1, char value2, char value3)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            while (c != value1 && c != value2 && c != value3)
            {
                if (position >= length)
                    return false;
                c = buffer[position++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUntil(out char c, char value1, char value2, char value3, char value4)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            while (c != value1 && c != value2 && c != value3 && c != value4)
            {
                if (position >= length)
                    return false;
                c = buffer[position++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadUntil(out char c, params char[] values)
        {
            if (position >= length)
            {
                c = default;
                return false;
            }
            c = buffer[position++];
            while (!values.Contains(c))
            {
                if (position >= length)
                    return false;
                c = buffer[position++];
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSpan(out ReadOnlySpan<char> s, int count)
        {
            if (position + count >= length)
            {
                s = default;
                return false;
            }
            s = buffer.Slice(position, count);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadString(out string s, int count)
        {
            if (position + count >= length)
            {
                s = default;
                return false;
            }
            s = buffer.Slice(position, count).ToString();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BackOne()
        {
            if (position == 0)
                throw new InvalidOperationException($"Cannot {nameof(BackOne)} before position of zero.");
            position--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginSegment(bool includeCurrent)
        {
            if (segmentStart != -1)
                return;
            segmentStart = position - (includeCurrent ? 1 : 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> EndSegmentToSpan(bool includeCurrent)
        {
            var segment = buffer.Slice(segmentStart, position - segmentStart - (includeCurrent ? 0 : 1));
            segmentStart = -1;
            return segment;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string EndSegmentToString(bool includeCurrent)
        {
            if (segmentStart == -1)
                return null;
            var segment = buffer.Slice(segmentStart, position - segmentStart - (includeCurrent ? 0 : 1)).ToString();
            segmentStart = -1;
            return segment;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndSegmentCopyTo(bool includeCurrent, ref CharWriter writer)
        {
            writer.Write(buffer.Slice(segmentStart, position - segmentStart - (includeCurrent ? 0 : 1)));
            segmentStart = -1;
        }
    }
}