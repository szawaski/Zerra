// Copyright � KaKush LLC
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

        [Fact]
        public void RemainingAndAdvance()
        {
            var buffer = new byte[10];
            var writer = new SpanWriter<byte>(buffer);
            writer.Write([1, 2]);

            Assert.Equal(8, writer.Remaining.Length);
            var written = System.Text.Encoding.UTF8.GetBytes("abc", writer.Remaining);
            writer.Advance(written);
            writer.Write([3]);

            Assert.Equal(6, writer.Position);
            Assert.True(buffer.AsSpan(0, 6).SequenceEqual(new byte[] { 1, 2, (byte)'a', (byte)'b', (byte)'c', 3 }));
        }

        [Fact]
        public void Advance_PastEnd_Throws()
        {
            var buffer = new byte[4];
            var writer = new SpanWriter<byte>(buffer);
            writer.Write([1, 2]);

            Exception exception = null;
            try
            {
                writer.Advance(3);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            _ = Assert.IsType<ArgumentOutOfRangeException>(exception);
        }
    }
}
