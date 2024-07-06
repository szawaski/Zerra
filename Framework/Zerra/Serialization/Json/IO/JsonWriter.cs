// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Zerra.IO;

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

        private bool useBytes;

        private int position;
        private readonly int length;

        public JsonWriter()
        {
            throw new NotSupportedException($"{nameof(JsonWriter)} cannot use default constructor");
        }

        public JsonWriter(bool useBytes, int initialSize)
        {
            if (useBytes)
            {
                this.bufferBytesOwner = BufferArrayPool<byte>.Rent(initialSize);
                this.bufferBytes = bufferBytesOwner;
            }
            else
            {
                this.bufferCharsOwner = BufferArrayPool<char>.Rent(initialSize);
                this.bufferChars = bufferCharsOwner;
            }
            this.position = 0;
            this.length = bufferChars.Length;
        }

        public JsonWriter(Span<char> buffer)
        {
            this.bufferChars = buffer;
            this.position = 0;
            this.length = buffer.Length;
        }

        public JsonWriter(Span<byte> buffer)
        {
            this.bufferBytes = buffer;
            this.position = 0;
            this.length = buffer.Length;
        }

        public readonly int Length => position;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferSize(int additionalSize)
        {
            if (useBytes)
            {
                if (position + additionalSize <= bufferChars.Length)
                    return;

                if (bufferCharsOwner == null)
                    throw new InvalidOperationException($"{nameof(JsonWriter)} has reached it's buffer limit");

                var minSize = position + additionalSize;
                BufferArrayPool<char>.Grow(ref bufferCharsOwner, minSize);
                bufferChars = bufferCharsOwner;
            }
            else
            {
                if (position + additionalSize <= bufferBytes.Length)
                    return;

                if (bufferBytesOwner == null)
                    throw new InvalidOperationException($"{nameof(JsonWriter)} has reached it's buffer limit");

                var minSize = position + additionalSize;
                BufferArrayPool<byte>.Grow(ref bufferBytesOwner, minSize);
                bufferBytes = bufferBytesOwner;
            }
        }

        public readonly Span<char> ToCharSpan()
        {
            return bufferChars.Slice(0, position);
        }
        public readonly char[] ToCharArray()
        {
            return bufferChars.Slice(0, position).ToArray();
        }
        public readonly Span<byte> ToByteSpan()
        {
            return bufferBytes.Slice(0, position);
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
            if (bufferCharsOwner != null)
            {
                Array.Clear(bufferCharsOwner, 0, position);
                BufferArrayPool<char>.Return(bufferCharsOwner);
                bufferCharsOwner = null;
                bufferChars = null;
            }
            if (bufferBytesOwner != null)
            {
                Array.Clear(bufferBytesOwner, 0, position);
                BufferArrayPool<byte>.Return(bufferBytesOwner);
                bufferBytesOwner = null;
                bufferBytes = null;
            }
        }
    }
}