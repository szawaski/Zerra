// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Text;
using Zerra.Serialization.Json.State;

namespace Zerra.Serialization.Json.IO
{
    /// <summary>
    /// A ref struct for efficiently reading JSON data from either a byte buffer or character buffer.
    /// Provides parsing capabilities and error reporting for JSON content.
    /// </summary>
    public ref partial struct JsonReader
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private const int errorHelperLength = 32;

        private readonly ReadOnlySpan<char> bufferChars;
        private readonly ReadOnlySpan<byte> bufferBytes;
        private readonly bool isFinalBlock;

        private readonly bool useBytes;

        /// <summary>
        /// Gets a value indicating whether the reader uses a byte buffer.
        /// </summary>
        public readonly bool UseBytes => useBytes;

        private int position;
        private readonly int length;

        /// <summary>
        /// Gets the current position in the buffer.
        /// </summary>
        public readonly int Position => position;

        /// <summary>
        /// Gets the total length of the buffer.
        /// </summary>
        public readonly int Length => length;

        /// <summary>
        /// Not supported default constructor.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public JsonReader()
        {
            throw new NotSupportedException($"{nameof(JsonReader)} cannot use default constructor");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReader"/> struct with a character buffer.
        /// </summary>
        /// <param name="chars">The character buffer to read from.</param>
        /// <param name="isFinalBlock">A value indicating whether this is the final block of data.</param>
        /// <param name="lastToken">The last JSON token that was read.</param>
        /// <remarks>
        /// This constructor is typically used when you have a JSON string that you want to read and parse.
        /// </remarks>
        public JsonReader(ReadOnlySpan<char> chars, bool isFinalBlock, JsonToken lastToken)
        {
            this.bufferChars = chars;
            this.position = 0;
            this.length = chars.Length;
            this.useBytes = false;
            this.isFinalBlock = isFinalBlock;
            this.Token = lastToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReader"/> struct with a byte buffer.
        /// </summary>
        /// <param name="bytes">The byte buffer to read from.</param>
        /// <param name="isFinalBlock">A value indicating whether this is the final block of data.</param>
        /// <param name="lastToken">The last JSON token that was read.</param>
        public JsonReader(ReadOnlySpan<byte> bytes, bool isFinalBlock, JsonToken lastToken)
        {
            this.bufferBytes = bytes;
            this.position = 0;
            this.length = bytes.Length;
            this.useBytes = true;
            this.isFinalBlock = isFinalBlock;
            this.Token = lastToken;
        }

        /// <summary>
        /// Creates a <see cref="FormatException"/> with a default error message and context information.
        /// </summary>
        /// <returns>A format exception with details about the current position and surrounding content.</returns>
        public readonly FormatException CreateException() => CreateException("Invalid JSON format");

        /// <summary>
        /// Creates a <see cref="FormatException"/> with the specified error message and context information.
        /// </summary>
        /// <param name="message">The error message to include in the exception.</param>
        /// <returns>A format exception with the message and details about the current position and surrounding content.</returns>
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