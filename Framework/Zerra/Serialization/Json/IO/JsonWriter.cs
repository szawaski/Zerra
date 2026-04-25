// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.Serialization.Json.IO
{
    /// <summary>
    /// A ref struct for efficiently writing JSON data to either a byte buffer or character buffer.
    /// Provides buffering and automatic growth capabilities for building JSON content.
    /// </summary>
    public ref partial struct JsonWriter
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private const int defaultBufferSize = 1024;

        private char[]? bufferCharsOwner;
        private Span<char> bufferChars;
        private byte[]? bufferBytesOwner;
        private Span<byte> bufferBytes;

        private readonly bool useBytes;

        private int position;
        private int length;

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
        private const byte quoteByte = (byte)'"';
        private const byte colonByte = (byte)':';
        private const byte commaByte = (byte)',';
        private const byte escapeByte = (byte)'\\';

        private const byte zeroByte = (byte)'0';
        private const byte oneByte = (byte)'1';
        private const byte twoByte = (byte)'2';
        private const byte threeByte = (byte)'3';
        private const byte fourByte = (byte)'4';
        private const byte fiveByte = (byte)'5';
        private const byte sixByte = (byte)'6';
        private const byte sevenByte = (byte)'7';
        private const byte eightByte = (byte)'8';
        private const byte nineByte = (byte)'9';
        private const byte dotByte = (byte)'.';
        private const byte minusByte = (byte)'-';
        private const byte plusByte = (byte)'+';
        private const byte zUpperByte = (byte)'Z';
        private const byte tUpperByte = (byte)'T';

        private const byte nByte = (byte)'n';
        private const byte uByte = (byte)'u';
        private const byte lByte = (byte)'l';
        private const byte tByte = (byte)'t';
        private const byte rByte = (byte)'r';
        private const byte eByte = (byte)'e';
        private const byte fByte = (byte)'f';
        private const byte aByte = (byte)'a';
        private const byte sByte = (byte)'s';
        private const byte bByte = (byte)'b';

        //invalid UTF8 surrogate characters
        private const char lowerSurrogate = (char)55296; //D800
        private const char upperSurrogate = (char)57343; //DFFF


#if DEBUG
        /// <summary>Enables debug testing mode to simulate partial writes.</summary>
        public static bool Testing = false;

        private bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DebugShouldReturn()
        {
            if (!JsonWriter.Testing)
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
        /// Gets a value indicating whether the writer uses a byte buffer.
        /// </summary>
        public readonly bool UseBytes => useBytes;

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
        public JsonWriter()
        {
            throw new NotSupportedException($"{nameof(JsonWriter)} cannot use default constructor");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> struct with a pooled buffer.
        /// </summary>
        /// <param name="useBytes">A value indicating whether to use a byte buffer; otherwise a character buffer is used.</param>
        /// <param name="initialSize">The initial size of the buffer.</param>
        public JsonWriter(bool useBytes, int initialSize)
        {
            if (useBytes)
            {
                this.bufferBytesOwner = ArrayPoolHelper<byte>.Rent(initialSize);
                this.bufferBytes = bufferBytesOwner;
            }
            else
            {
                this.bufferCharsOwner = ArrayPoolHelper<char>.Rent(initialSize);
                this.bufferChars = bufferCharsOwner;
            }
            this.position = 0;
            this.length = bufferChars.Length;
            this.useBytes = useBytes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> struct with a provided byte buffer.
        /// </summary>
        /// <param name="buffer">The byte buffer to write to.</param>
        public JsonWriter(Span<byte> buffer)
        {
            this.bufferBytes = buffer;
            this.position = 0;
            this.length = buffer.Length;
            this.useBytes = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Grow(int sizeNeeded)
        {
            if (useBytes)
            {
                if (bufferBytesOwner is null)
                    return false;

                ArrayPoolHelper<byte>.Grow(ref bufferBytesOwner, Math.Max(bufferBytesOwner.Length * 2, bufferBytesOwner.Length + sizeNeeded));
                bufferBytes = bufferBytesOwner;
                length = bufferBytesOwner.Length;
            }
            else
            {
                if (bufferCharsOwner is null)
                    return false;

                ArrayPoolHelper<char>.Grow(ref bufferCharsOwner, Math.Max(bufferCharsOwner.Length * 2, bufferCharsOwner.Length + sizeNeeded));
                bufferChars = bufferCharsOwner;
                length = bufferCharsOwner.Length;
            }
            return true;
        }

        /// <summary>
        /// Converts the written content to a byte array.
        /// </summary>
        /// <returns>A byte array containing the written data.</returns>
        public readonly byte[] ToByteArray()
        {
            return bufferBytes.Slice(0, position).ToArray();
        }

        /// <summary>
        /// Converts the written content to a string.
        /// </summary>
        /// <returns>A string containing the written data.</returns>
        /// <exception cref="NotSupportedException">Thrown when the writer uses a byte buffer instead of a character buffer.</exception>
        public override readonly string ToString()
        {
            if (useBytes)
                throw new NotSupportedException();

            return bufferChars.Slice(0, position).ToString();
        }

        /// <summary>
        /// Releases the pooled buffers back to the array pool.
        /// </summary>
        public void Dispose()
        {
            if (bufferCharsOwner is not null)
            {
                ArrayPoolHelper<char>.Return(bufferCharsOwner);
                bufferCharsOwner = null;
                bufferChars = null;
            }
            if (bufferBytesOwner is not null)
            {
                ArrayPoolHelper<byte>.Return(bufferBytesOwner);
                bufferBytesOwner = null;
                bufferBytes = null;
            }
        }
    }
}