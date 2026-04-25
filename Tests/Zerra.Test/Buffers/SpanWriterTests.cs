// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Buffers;

namespace Zerra.Test.Buffers
{
    public class SpanWriterTests
    {
        [Fact]
        public void Write()
        {
            var buffer = new byte[20];
            var writer = new SpanWriter<byte>(buffer);
            writer.Write([1, 2, 3, 4, 5]);
            writer.Write([6, 7, 8, 9, 10]);
            writer.Write([11, 12, 13, 14, 15]);
            writer.Write([16, 17, 18, 19, 20]);
            Assert.Equal(20, writer.Position);
            Assert.True(buffer.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }));
        }

        [Fact]
        public void Overflow()
        {
            var buffer = new byte[10];
            var writer = new SpanWriter<byte>(buffer);
            writer.Write([1, 2, 3, 4, 5]);
            writer.Write([6, 7, 8, 9, 10]);
            Exception exception = null;
            try
            {
                writer.Write([11, 12, 13, 14, 15]);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.NotNull(exception);
            _ = Assert.IsType<ArgumentException>(exception);
        }
    }
}
