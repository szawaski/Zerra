// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.IO
{
    public ref partial struct CharReader
    {
        const int errorHelperLength = 32;

        private readonly ReadOnlySpan<char> buffer;

        private int position;
        private readonly int length;

        public readonly int Position => position;
        public readonly int Length => length;

        public CharReader(string chars)
        {
            this.buffer = chars.AsSpan();
            this.position = 0;
            this.length = this.buffer.Length;
        }
        public CharReader(ReadOnlySpan<char> chars)
        {
            this.buffer = chars;
            this.position = 0;
            this.length = this.buffer.Length;
        }

        public readonly bool HasMoreChars()
        {
            if (length - position > 0)
                return true;

            return false;
        }

        public readonly FormatException CreateException(string message)
        {
            var character = buffer[position];

            var start1 = (position - 1) > errorHelperLength ? (position - 1) - errorHelperLength : 0;
            var length1 = start1 + errorHelperLength > (position - 1) ? (position - 1) - start1 : errorHelperLength;
            var helper1 = buffer.Slice(start1, length1).ToString();

            var start2 = position + 1;
            var length2 = start2 + errorHelperLength > buffer.Length ? buffer.Length - start2 : errorHelperLength;
            var helper2 = buffer.Slice(start2, length2).ToString();

            return new FormatException($"JSON Error: {message} at position {position} character {character} between {helper1} and {helper2}");
        }
    }
}