// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Compression
{
    /// <summary>
    /// Indicates a compression algorithm.
    /// </summary>
    public enum CompressionAlgorithmType : byte
    {
        /// <summary>
        /// Raw Deflate (RFC 1951) with no header or checksum.
        /// </summary>
        Deflate,
        /// <summary>
        /// GZip (RFC 1952), Deflate with a header and CRC-32 checksum.
        /// </summary>
        GZip,
        /// <summary>
        /// ZLib (RFC 1950), Deflate with a small header and Adler-32 checksum.
        /// Not supported on .NET Standard 2.0.
        /// </summary>
        ZLib,
        /// <summary>
        /// Brotli (RFC 7932), usually smaller output than Deflate at a higher CPU cost.
        /// Not supported on .NET Standard 2.0.
        /// </summary>
        Brotli
    }
}
