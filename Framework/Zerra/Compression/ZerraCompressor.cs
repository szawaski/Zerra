// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.IO.Compression;
using Zerra.IO;

namespace Zerra.Compression
{
    /// <summary>
    /// Provides compression and decompression functionality using a configurable algorithm and level.
    /// </summary>
    public sealed class ZerraCompressor : ICompressor
    {
        private const CompressionLevel defaultLevel = CompressionLevel.Fastest;
        private const int resultStartLength = 256;

        private readonly CompressionAlgorithmType algorithm;
        private readonly CompressionLevel level;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZerraCompressor"/> class.
        /// </summary>
        /// <param name="algorithm">The compression algorithm to use for compression and decompression.</param>
        /// <param name="level">The compression level, default is Fastest. Only affects compression.</param>
        /// <exception cref="PlatformNotSupportedException">Thrown on .NET Standard 2.0 for ZLib and Brotli.</exception>
        public ZerraCompressor(CompressionAlgorithmType algorithm, CompressionLevel level = defaultLevel)
        {
            switch (algorithm)
            {
                case CompressionAlgorithmType.Deflate:
                case CompressionAlgorithmType.GZip:
                    break;
                case CompressionAlgorithmType.ZLib:
                case CompressionAlgorithmType.Brotli:
#if NETSTANDARD2_0
                    throw new PlatformNotSupportedException($"{algorithm} compression is not supported on .NET Standard 2.0");
#else
                    break;
#endif
                default:
                    throw new NotSupportedException($"{nameof(CompressionAlgorithmType)} {algorithm} not supported");
            }
            this.algorithm = algorithm;
            this.level = level;
        }

        /// <inheritdoc/>
        public byte[] Compress(byte[] bytes)
        {
            using (var resultStream = new PooledWriteStream(bytes.Length / 2 + resultStartLength))
            {
                using (var compressStream = Compress(resultStream, true))
                {
                    compressStream.Write(bytes, 0, bytes.Length);
                }
                return resultStream.ToArray();
            }
        }

        /// <inheritdoc/>
        public byte[] Decompress(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            using (var decompressStream = Decompress(memoryStream, true))
            {
                return decompressStream.ToArray();
            }
        }

#if !NETSTANDARD2_0
        /// <inheritdoc/>
        public Span<byte> Compress(ReadOnlySpan<byte> bytes)
        {
            using (var resultStream = new PooledWriteStream(bytes.Length / 2 + resultStartLength))
            {
                using (var compressStream = Compress(resultStream, true))
                {
                    compressStream.Write(bytes);
                }
                return resultStream.ToArray();
            }
        }

        /// <inheritdoc/>
        public unsafe Span<byte> Decompress(ReadOnlySpan<byte> bytes)
        {
            //read in place, the span is pinned until the result is copied out
            fixed (byte* pBytes = bytes)
            {
                using (var memoryStream = new UnmanagedMemoryStream(pBytes, bytes.Length))
                using (var decompressStream = Decompress(memoryStream, true))
                {
                    return decompressStream.ToArray();
                }
            }
        }
#endif

        /// <inheritdoc/>
        public Stream Compress(Stream stream, bool leaveOpen)
        {
            return algorithm switch
            {
                CompressionAlgorithmType.Deflate => new DeflateStream(stream, level, leaveOpen),
                CompressionAlgorithmType.GZip => new GZipStream(stream, level, leaveOpen),
#if !NETSTANDARD2_0
                CompressionAlgorithmType.ZLib => new ZLibStream(stream, level, leaveOpen),
                CompressionAlgorithmType.Brotli => new BrotliStream(stream, level, leaveOpen),
#endif
                _ => throw new NotSupportedException($"{nameof(CompressionAlgorithmType)} {algorithm} not supported"),
            };
        }

        /// <inheritdoc/>
        public Stream Decompress(Stream stream, bool leaveOpen)
        {
            return algorithm switch
            {
                CompressionAlgorithmType.Deflate => new DeflateStream(stream, CompressionMode.Decompress, leaveOpen),
                CompressionAlgorithmType.GZip => new GZipStream(stream, CompressionMode.Decompress, leaveOpen),
#if !NETSTANDARD2_0
                CompressionAlgorithmType.ZLib => new ZLibStream(stream, CompressionMode.Decompress, leaveOpen),
                CompressionAlgorithmType.Brotli => new BrotliStream(stream, CompressionMode.Decompress, leaveOpen),
#endif
                _ => throw new NotSupportedException($"{nameof(CompressionAlgorithmType)} {algorithm} not supported"),
            };
        }
    }
}
