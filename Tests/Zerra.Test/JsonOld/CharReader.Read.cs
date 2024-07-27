// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.Serialization.Json
{
    public ref partial struct CharReaderOld
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
            while (Char.IsWhiteSpace(c))
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
            if (position + count > length)
            {
                s = default;
                return false;
            }
            s = buffer.Slice(position, count);
            position += count;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSpanUntil(out ReadOnlySpan<char> s, char value)
        {
            if (position >= length)
            {
                s = default;
                return false;
            }
            var start = position;
            var p = position;
            var c = buffer[p++];
            while (c != value)
            {
                if (p >= length)
                {
                    s = default;
                    return false;
                }
                c = buffer[p++];
            }
            s = buffer.Slice(start, p - start);
            position = p;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSpanUntil(out ReadOnlySpan<char> s, char value1, char value2)
        {
            if (position >= length)
            {
                s = default;
                return false;
            }
            var start = position;
            var p = position;
            var c = buffer[p++];
            while (c != value1 && c != value2)
            {
                if (p >= length)
                {
                    s = default;
                    return false;
                }
                c = buffer[p++];
            }
            s = buffer.Slice(start, p - start);
            position = p;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSpanUntil(out ReadOnlySpan<char> s, char value1, char value2, char value3)
        {
            if (position >= length)
            {
                s = default;
                return false;
            }
            var start = position;
            var p = position;
            var c = buffer[p++];
            while (c != value1 && c != value2 && c != value3)
            {
                if (p >= length)
                {
                    s = default;
                    return false;
                }
                c = buffer[p++];
            }
            s = buffer.Slice(start, p - start);
            position = p;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadSpanUntil(out ReadOnlySpan<char> s, char value1, char value2, char value3, char value4)
        {
            if (position >= length)
            {
                s = default;
                return false;
            }
            var start = position;
            var p = position;
            var c = buffer[p++];
            while (c != value1 && c != value2 && c != value3 && c != value4)
            {
                if (p >= length)
                {
                    s = default;
                    return false;
                }
                c = buffer[p++];
            }
            s = buffer.Slice(start, p - start);
            position = p;
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
        public void Back(int count)
        {
            if (position - count < 0)
                throw new InvalidOperationException($"Cannot {nameof(BackOne)} before position of zero.");
            position--;
        }
    }
}