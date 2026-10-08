// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Compression;

namespace Zerra.Test.Compression
{
    public class PooledWriteStreamTests
    {
        [Fact]
        public void WritesGrowAndCopy()
        {
            var stream = new PooledWriteStream(4);
            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.True(stream.CanWrite);

            stream.Write([1, 2, 3], 0, 3);
            stream.Write([4, 5, 6]);
            stream.WriteByte(7);
            stream.Write(Enumerable.Range(0, 100).Select(x => (byte)x).ToArray());
            stream.Flush();

            Assert.Equal(107, stream.Length);
            Assert.Equal(107, stream.Position);
            var bytes = stream.ToArray();
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, bytes[..7]);
            Assert.Equal(99, bytes[106]);

            _ = Assert.Throws<NotSupportedException>(() => stream.Read(new byte[1], 0, 1));
            _ = Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            _ = Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
            _ = Assert.Throws<NotSupportedException>(() => stream.Position = 0);

            stream.Dispose();
            stream.Dispose();
            _ = Assert.Throws<ObjectDisposedException>(() => stream.Write([1], 0, 1));
            _ = Assert.Throws<ObjectDisposedException>(() => stream.Write([1]));
            _ = Assert.Throws<ObjectDisposedException>(() => stream.WriteByte(1));
            _ = Assert.Throws<ObjectDisposedException>(() => stream.ToArray());
        }
    }
}