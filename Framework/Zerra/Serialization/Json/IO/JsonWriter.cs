// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Runtime.CompilerServices;
using System.Text;
using Zerra.Buffers;

namespace Zerra.Serialization.Json.IO
{
    public ref partial struct JsonWriter
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private const int defaultBufferSize = 1024;

        private char[]? bufferCharsOwner;
        private Span<char> bufferChars;
        private byte[]? bufferBytesOwner;
        private Span<byte> bufferBytes;

        private readonly bool useBytes;
        public readonly bool UseBytes => useBytes;

        private int position;
        private int length;

        public readonly int Position => position;
        public readonly int Length => length;

        public JsonWriter()
        {
            throw new NotSupportedException($"{nameof(JsonWriter)} cannot use default constructor");
        }

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

        public readonly byte[] ToByteArray()
        {
            return bufferBytes.Slice(0, position).ToArray();
        }
        public override readonly string ToString()
        {
            if (useBytes)
                throw new NotSupportedException();

            return bufferChars.Slice(0, position).ToString();
        }

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