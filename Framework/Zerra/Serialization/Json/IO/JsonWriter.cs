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

        /// <summary>
        /// Gets a value indicating whether the writer uses a byte buffer.
        /// </summary>
        public readonly bool UseBytes => useBytes;

        private int position;
        private int length;

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