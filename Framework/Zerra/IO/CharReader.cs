// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.IO
{
    public ref partial struct CharReader
    {
        const int errorHelperLength = 32;

        private ReadOnlySpan<char> buffer;

        private int position;
        private int length;

        public int Position => position;
        public int Length => length;

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

        public bool HasMoreChars()
        {
            if (length - position > 0)
                return true;

            return false;
        }

        public FormatException CreateException(string message)
        {
            var start = position > errorHelperLength ? position - errorHelperLength - 2 : 0;
            var length = start + errorHelperLength > position ? position - start : errorHelperLength;
            var helper = buffer.Slice(start, length).ToString();
            return new FormatException($"JSON Error: {message} at position {position} character after {helper}");
        }
    }
}