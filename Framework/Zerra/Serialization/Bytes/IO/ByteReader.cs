// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.Serialization.Bytes.IO
{
    /// <summary>
    /// A ref struct for efficiently reading binary data from a byte buffer.
    /// Provides parsing capabilities for binary content.
    /// </summary>
    public ref partial struct ByteReader
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private const byte nullByte = 0;

        private readonly ReadOnlySpan<byte> buffer;

        private int position;
        private readonly int length;

#if DEBUG
        /// <summary>Enables debug testing mode to simulate partial reads.</summary>
        public static bool Testing = false;

        /// <summary>Tracks alternating state used during debug testing.</summary>
        public bool Alternate = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DebugShouldReturn()
        {
            if (!ByteReader.Testing)
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
        public ByteReader()
        {
            throw new NotSupportedException($"{nameof(ByteReader)} cannot use default constructor");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteReader"/> struct with a byte buffer.
        /// </summary>
        /// <param name="bytes">The byte buffer to read from.</param>
        public ByteReader(ReadOnlySpan<byte> bytes)
        {
            this.buffer = bytes;
            this.position = 0;
            this.length = bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool SeekNullableSizeNeeded(int collectionLength, int sizePerElement, ref int sizeNeeded)
        {
            var tempPosition = position;
            fixed (byte* pBuffer = buffer)
            {
                for (var i = 0; i < collectionLength; i++)
                {
                    sizeNeeded += 1;
                    if (length - tempPosition < sizeNeeded)
                        return false;
                    if (pBuffer[tempPosition++] is not nullByte)
                    {
                        sizeNeeded += sizePerElement;
                        tempPosition += sizePerElement;
                        if (length - tempPosition < sizeNeeded)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}
