// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Repository.IO
{
    public ref partial struct CharWriter
    {
        /// <summary>
        /// Specifies the text format to use when writing a raw byte array.
        /// </summary>
        public enum ByteFormat : byte
        {
            /// <summary>Writes each byte as a two-character lowercase hexadecimal string.</summary>
            Hex
        }
    }
}