// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Buffers;
using System.Runtime.CompilerServices;
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

        //https://www.rfc-editor.org/rfc/rfc4627

        //last byte 128 to 191
        //1 bytes: 0 to 127
        //2 bytes: 192 to ?
        //3 bytes: 224 to ?
        //4 bytes: 240 to ?

        private const byte openBracketByte = (byte)'[';
        private const byte closeBracketByte = (byte)']';
        private const byte openBraceByte = (byte)'{';
        private const byte closeBraceByte = (byte)'}';
        private const byte commaByte = (byte)',';
        private const byte colonByte = (byte)':';
        private const byte quoteByte = (byte)'"';
        private const byte escapeByte = (byte)'\\';

        private static readonly byte[] ullBytes = [(byte)'u', (byte)'l', (byte)'l'];
        private static readonly byte[] rueBytes = [(byte)'r', (byte)'u', (byte)'e'];
        private static readonly byte[] alseBytes = [(byte)'a', (byte)'l', (byte)'s', (byte)'e'];

        private static readonly char[] ullChars = ['u', 'l', 'l'];
        private static readonly char[] rueChars = ['r', 'u', 'e'];
        private static readonly char[] alseChars = ['a', 'l', 's', 'e'];

        private static readonly SearchValues<byte> quoteEscapeBytes = SearchValues.Create((byte)'"', (byte)'\\');
        private static readonly SearchValues<char> quoteEscapeChars = SearchValues.Create('"', '\\');

        private const byte uByte = (byte)'u';
        private const byte bByte = (byte)'b';
        private const byte tByte = (byte)'t';
        private const byte nByte = (byte)'n';
        private const byte fByte = (byte)'f';
        private const byte rByte = (byte)'r';

        //Numbers
        private static readonly char[] numberChars = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '-', '.', 'e', 'E'];
        private static readonly byte[] numberBytes = [(byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'+', (byte)'-', (byte)'.', (byte)'e', (byte)'E'];
        private const byte eByte = (byte)'e';
        private const byte eUpperByte = (byte)'E';

        private const byte plusByte = (byte)'+'; //43
        private const byte minusByte = (byte)'-'; //45
        private const byte dotByte = (byte)'.'; //46
        private const byte zeroByte = (byte)'0'; //48
        private const byte nineByte = (byte)'9'; //57

        //JSON whitespace
        private const byte spaceByte = (byte)' '; //32
        private const byte tabByte = (byte)'\t'; //9
        private const byte returnByte = (byte)'\r'; //13
        private const byte newlineByte = (byte)'\n'; //10
        private static readonly byte[] whiteSpaceBytes = [(byte)' ', (byte)'\t', (byte)'\r', (byte)'\n'];
        private static readonly char[] whiteSpaceChars = [' ', '\t', '\r', '\n'];

#if DEBUG
        /// <summary>
        /// Enables debug testing mode to simulate partial reads.
        /// </summary>
        public static bool Testing = false;

        /// <summary>
        /// Tracks alternating state used during debug testing.
        /// </summary>
        public bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DebugShouldReturn()
        {
            if (!JsonReader.Testing)
                return false;
            if (Alternate)
            {
                Alternate = false;
                return false;
            }
            else
            {
                Alternate = true;
                return true;
            }
        }
#endif

        /// <summary>
        /// Gets the last read token.
        /// </summary>
        public JsonToken Token;
        /// <summary>
        /// Gets the position of the first escape character.
        /// </summary>
        public int PositionOfFirstEscape = -1;
        /// <summary>
        /// Gets the bytes of the last read value.
        /// </summary>
        public ReadOnlySpan<byte> ValueBytes;
        /// <summary>
        /// Gets the characters of the last read value.
        /// </summary>
        public ReadOnlySpan<char> ValueChars;


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