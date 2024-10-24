// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.Serialization.Json.IO
{
    public ref partial struct JsonReader
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        const int errorHelperLength = 32;

        private readonly ReadOnlySpan<char> bufferChars;
        private readonly ReadOnlySpan<byte> bufferBytes;
        private bool useBytes;

        private int position;
        private readonly int length;

        public readonly int Position => position;
        public readonly int Length => length;

        public JsonReader(ReadOnlySpan<char> chars)
        {
            this.bufferChars = chars;
            this.position = 0;
            this.length = chars.Length;
            this.useBytes = false;
        }

        public JsonReader(ReadOnlySpan<byte> bytes)
        {
            this.bufferBytes = bytes;
            this.position = 0;
            this.length = bytes.Length;
            this.useBytes = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BackOne()
        {
            if (position == 0)
                throw new InvalidOperationException($"Cannot {nameof(BackOne)} before position of zero.");
            position--;
        }

        public readonly FormatException CreateException(string message)
        {
            var charPostion = Math.Max(position - 1, 0);

            if (useBytes)
            {
                if (bufferBytes.Length - charPostion < 4)
                    return new FormatException($"JSON Error: {message}");
              
#if NETSTANDARD2_0
                var character = encoding.GetString(bufferBytes.Slice(charPostion, 4).ToArray())[0];
#else
                var character = encoding.GetString(bufferBytes.Slice(charPostion, 4))[0];
#endif

                var start1 = charPostion > errorHelperLength ? charPostion - errorHelperLength : 0;
                var length1 = start1 + errorHelperLength > charPostion ? charPostion - start1 : errorHelperLength;
#if NETSTANDARD2_0
                var helper1 = encoding.GetString(bufferBytes.Slice(start1, length1).ToArray());
#else
                var helper1 = encoding.GetString(bufferBytes.Slice(start1, length1));
#endif

                var start2 = charPostion + 1;
                var length2 = start2 + errorHelperLength > bufferBytes.Length ? bufferBytes.Length - start2 : errorHelperLength;
#if NETSTANDARD2_0
                var helper2 = encoding.GetString(bufferBytes.Slice(start2, length2).ToArray());
#else
                var helper2 = encoding.GetString(bufferBytes.Slice(start2, length2));
#endif

                return new FormatException($"JSON Error: {message} at position {position} character ~{character}~ between ~{helper1}~ and ~{helper2}~");
            }
            else
            {
                var character = bufferChars[charPostion];

                var start1 = charPostion > errorHelperLength ? charPostion - errorHelperLength : 0;
                var length1 = start1 + errorHelperLength > charPostion ? charPostion - start1 : errorHelperLength;
                var helper1 = bufferChars.Slice(start1, length1).ToString();

                var start2 = position + 1;
                var length2 = start2 + errorHelperLength > bufferChars.Length ? bufferChars.Length - start2 : errorHelperLength;
                var helper2 = bufferChars.Slice(start2, length2).ToString();

                return new FormatException($"JSON Error: {message} at position {position} character {character} between {helper1} and {helper2}");
            }
        }
    }
}