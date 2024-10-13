// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Text;

namespace Zerra.Serialization.Bytes.IO
{
    public ref partial struct ByteReaderOld
    {
        private static readonly Encoding defaultEncoding = Encoding.Unicode;

        private const byte nullByte = 0;

        private readonly ReadOnlySpan<byte> buffer;

        private int position;
        private readonly int length;

        private readonly Encoding encoding;

        public readonly int Position => position;
        public readonly int Length => length;

        public ByteReaderOld(ReadOnlySpan<byte> bytes, Encoding? encoding = null)
        {
            this.buffer = bytes;
            this.encoding = encoding ?? defaultEncoding;
            this.position = 0;
            this.length = bytes.Length;
        }
    }
}
