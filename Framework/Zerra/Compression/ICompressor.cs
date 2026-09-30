// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Compression
{
    /// <summary>
    /// Defines compression and decompression operations for byte arrays and streams.
    /// </summary>
    /// <remarks>
    /// Provides methods to compress and decompress data in various formats: byte arrays, spans, and streams.
    /// Implementations handle the actual compression algorithm.
    /// </remarks>
    public interface ICompressor
    {
        /// <summary>
        /// Compresses a byte array.
        /// </summary>
        /// <param name="bytes">The bytes to compress.</param>
        /// <returns>The compressed bytes.</returns>
        byte[] Compress(byte[] bytes);

        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="bytes">The compressed bytes to decompress.</param>
        /// <returns>The decompressed bytes.</returns>
        byte[] Decompress(byte[] bytes);

#if !NETSTANDARD2_0
        /// <summary>
        /// Compresses a byte span.
        /// </summary>
        /// <remarks>
        /// Available on .NET 5.0 and later (not available on .NET Standard 2.0).
        /// </remarks>
        /// <param name="bytes">The bytes to compress.</param>
        /// <returns>A span containing the compressed bytes.</returns>
        Span<byte> Compress(ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Decompresses a byte span.
        /// </summary>
        /// <remarks>
        /// Available on .NET 5.0 and later (not available on .NET Standard 2.0).
        /// </remarks>
        /// <param name="bytes">The compressed bytes to decompress.</param>
        /// <returns>A span containing the decompressed bytes.</returns>
        Span<byte> Decompress(ReadOnlySpan<byte> bytes);
#endif

        /// <summary>
        /// Creates a compression stream wrapper for writing.
        /// </summary>
        /// <remarks>
        /// Data written to the returned stream is compressed into <paramref name="stream"/>.
        /// The compressed data is only complete once the returned stream is disposed.
        /// </remarks>
        /// <param name="stream">The underlying stream that receives the compressed data.</param>
        /// <param name="leaveOpen">True to leave <paramref name="stream"/> open when the returned stream is disposed.</param>
        /// <returns>A stream that compresses data transparently.</returns>
        Stream Compress(Stream stream, bool leaveOpen);

        /// <summary>
        /// Creates a decompression stream wrapper for reading.
        /// </summary>
        /// <remarks>
        /// Data read from the returned stream is decompressed from <paramref name="stream"/>.
        /// </remarks>
        /// <param name="stream">The underlying stream containing the compressed data.</param>
        /// <param name="leaveOpen">True to leave <paramref name="stream"/> open when the returned stream is disposed.</param>
        /// <returns>A stream that decompresses data transparently.</returns>
        Stream Decompress(Stream stream, bool leaveOpen);
    }
}
