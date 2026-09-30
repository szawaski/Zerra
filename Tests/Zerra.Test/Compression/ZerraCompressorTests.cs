// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Compression;

namespace Zerra.Test.Compression
{
    public class ZerraCompressorTests
    {
        private static byte[] GetTestBytes()
        {
            var bytes = new byte[100_000];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(i % 17);
            return bytes;
        }

        [Theory]
        [InlineData(CompressionAlgorithmType.Deflate)]
        [InlineData(CompressionAlgorithmType.GZip)]
        [InlineData(CompressionAlgorithmType.ZLib)]
        [InlineData(CompressionAlgorithmType.Brotli)]
        public void Bytes_RoundTrip(CompressionAlgorithmType algorithm)
        {
            var compressor = new ZerraCompressor(algorithm);
            var test = GetTestBytes();

            var compressed = compressor.Compress(test);
            Assert.True(compressed.Length < test.Length / 10);

            var result = compressor.Decompress(compressed);
            Assert.Equal(test, result);
        }

        [Theory]
        [InlineData(CompressionAlgorithmType.Deflate)]
        [InlineData(CompressionAlgorithmType.GZip)]
        [InlineData(CompressionAlgorithmType.ZLib)]
        [InlineData(CompressionAlgorithmType.Brotli)]
        public void Span_RoundTrip(CompressionAlgorithmType algorithm)
        {
            var compressor = new ZerraCompressor(algorithm);
            var test = GetTestBytes();

            var compressed = compressor.Compress(test.AsSpan()).ToArray();
            var result = compressor.Decompress((ReadOnlySpan<byte>)compressed).ToArray();

            Assert.Equal(test, result);
            Assert.Equal(test, compressor.Decompress(compressed));
        }

        [Theory]
        [InlineData(CompressionAlgorithmType.Deflate)]
        [InlineData(CompressionAlgorithmType.GZip)]
        [InlineData(CompressionAlgorithmType.ZLib)]
        [InlineData(CompressionAlgorithmType.Brotli)]
        public void Empty_RoundTrip(CompressionAlgorithmType algorithm)
        {
            var compressor = new ZerraCompressor(algorithm);

            var compressed = compressor.Compress(Array.Empty<byte>());
            var result = compressor.Decompress(compressed);

            Assert.Empty(result);
        }

        [Theory]
        [InlineData(CompressionAlgorithmType.Deflate)]
        [InlineData(CompressionAlgorithmType.GZip)]
        [InlineData(CompressionAlgorithmType.ZLib)]
        [InlineData(CompressionAlgorithmType.Brotli)]
        public async Task Stream_RoundTrip(CompressionAlgorithmType algorithm)
        {
            var compressor = new ZerraCompressor(algorithm);
            var test = GetTestBytes();

            using var compressedStream = new MemoryStream();
            await using (var compressStream = compressor.Compress(compressedStream, true))
            {
                await compressStream.WriteAsync(test, TestContext.Current.CancellationToken);
            }
            Assert.True(compressedStream.CanRead); //left open

            compressedStream.Position = 0;
            using var resultStream = new MemoryStream();
            await using (var decompressStream = compressor.Decompress(compressedStream, true))
            {
                await decompressStream.CopyToAsync(resultStream, TestContext.Current.CancellationToken);
            }

            Assert.Equal(test, resultStream.ToArray());
        }

        [Fact]
        public void Stream_Decompress_DisposesSource()
        {
            var compressor = new ZerraCompressor(CompressionAlgorithmType.GZip);
            var source = new MemoryStream(compressor.Compress(GetTestBytes()));

            compressor.Decompress(source, false).Dispose();

            Assert.False(source.CanRead);
        }

        [Fact]
        public void Stream_Compress_LeavesOpen()
        {
            var compressor = new ZerraCompressor(CompressionAlgorithmType.GZip);
            var destination = new MemoryStream();

            compressor.Compress(destination, false).Dispose();
            Assert.False(destination.CanWrite);

            destination = new MemoryStream();
            compressor.Compress(destination, true).Dispose();
            Assert.True(destination.CanWrite);
        }
    }
}
