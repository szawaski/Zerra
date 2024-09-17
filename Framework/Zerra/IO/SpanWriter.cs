// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Runtime.CompilerServices;

namespace Zerra.IO
{
    public ref struct SpanWriter
    {
        private readonly Span<byte> buffer;

        private int position;
        public readonly int Position => position;

        public SpanWriter(Span<byte> buffer)
        {
            this.buffer = buffer;
            this.position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(byte[] bytes)
        {
            if (bytes.Length == 0)
                return;
            if (position + bytes.Length <= buffer.Length)
                throw new InvalidOperationException($"{nameof(SpanWriter)} cannot excede the length of the Span");
            fixed (byte* pBuffer = &buffer[position], pBytes = &bytes[0])
            {
                //Buffer.MemoryCopy(pBytes, pBuffer, buffer.Length - position, bytes.Length);
                for (var i = 0; i < bytes.Length; i++)
                {
                    pBuffer[i] = pBytes[i];
                }
            }
            position += bytes.Length;
        }
    }
}