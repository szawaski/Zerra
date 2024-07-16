// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteReader
    {
        private const byte nullByte = 0;

        private readonly ReadOnlySpan<byte> buffer;

        private int position;
        private readonly int length;

        private readonly Encoding encoding;

        public readonly int Position => position;
        public readonly int Length => length;

        public ByteReader()
        {
            throw new NotSupportedException($"{nameof(ByteReader)} cannot use default constructor");
        }

        public ByteReader(ReadOnlySpan<byte> bytes, Encoding encoding)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            this.buffer = bytes;
            this.encoding = encoding;
            this.position = 0;
            this.length = bytes.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SeekNullableSizeNeeded(int length, int sizePerElement, out int sizeNeeded)
        {
            if (length == 0)
            {
                sizeNeeded = 0;
                return true;
            }

            sizeNeeded = 0;
            var tempPosition = position;

            for (var i = 0; i < length; i++)
            {
                sizeNeeded += 1;
                if (this.length - position < sizeNeeded)
                    return false;
                if (buffer[tempPosition++] != nullByte)
                {
                    sizeNeeded += sizePerElement;
                    tempPosition += sizePerElement;
                    if (this.length - position < sizeNeeded)
                        return false;
                }    
            }

            return true;
        }
    }
}
