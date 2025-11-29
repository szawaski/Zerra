// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteReader
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private const byte nullByte = 0;

        private readonly ReadOnlySpan<byte> buffer;

        private int position;
        private readonly int length;

        public readonly int Position => position;
        public readonly int Length => length;

        public ByteReader()
        {
            throw new NotSupportedException($"{nameof(ByteReader)} cannot use default constructor");
        }

        public ByteReader(ReadOnlySpan<byte> bytes)
        {
            this.buffer = bytes;
            this.position = 0;
            this.length = bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool SeekNullableSizeNeeded(int collectionLength, int sizePerElement, ref int sizeNeeded)
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
