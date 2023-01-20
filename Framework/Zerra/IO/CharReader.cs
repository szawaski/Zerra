﻿// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.IO
{
    public ref partial struct CharReader
    {
        private ReadOnlySpan<char> buffer;

        private int position;
        private int length;

        public int Position => position;
        public int Length => length;

        private int segmentStart;

        public CharReader(string chars)
        {
            this.buffer = chars.AsSpan();
            this.position = 0;
            this.length = this.buffer.Length;
            this.segmentStart = -1;
        }
        public CharReader(ReadOnlySpan<char> chars)
        {
            this.buffer = chars;
            this.position = 0;
            this.length = this.buffer.Length;
            this.segmentStart = -1;
        }

        public bool HasMoreChars()
        {
            if (length - position > 0)
                return true;

            return false;
        }

        public FormatException CreateException(string message)
        {
            const int helperLength = 32;
            var start = position > helperLength ? position - helperLength - 2 : 0;
            var length = start + helperLength > position ? position - start : helperLength;
            var helper = buffer.Slice(start, length).ToString();
            return new FormatException($"JSON Error: {message} at position {position} character after {helper}");
        }
    }
}