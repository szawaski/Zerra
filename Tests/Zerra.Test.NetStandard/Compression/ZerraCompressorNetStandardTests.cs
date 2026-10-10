// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Compression;

namespace Zerra.Test.NetStandard.Compression
{
    public class ZerraCompressorNetStandardTests
    {
        [Theory]
        [InlineData(CompressionAlgorithmType.ZLib)]
        [InlineData(CompressionAlgorithmType.Brotli)]
        public void NotSupported(CompressionAlgorithmType algorithm)
        {
            _ = Assert.Throws<PlatformNotSupportedException>(() => new ZerraCompressor(algorithm));
        }
    }
}
