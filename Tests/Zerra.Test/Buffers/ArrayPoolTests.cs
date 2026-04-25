// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Buffers;

namespace Zerra.Test.Buffers
{
    public class ArrayPoolTests
    {
        [Fact]
        public void RentGrowReturn()
        {
            var minSize = 4;
            var buffer = ArrayPoolHelper<byte>.Rent(minSize);
            var length = buffer.Length;
            Assert.True(length >= minSize);

            for (int i = 0; i < length; i++)
                Assert.Equal(0, buffer[i]);
            for (int i = 0; i < length; i++)
                buffer[i] = 1;

            var newMinSize = buffer.Length * 2;
            ArrayPoolHelper<byte>.Grow(ref buffer, newMinSize);
            var newLength = buffer.Length;

            Assert.True(newLength >= newMinSize);
            for (int i = 0; i < length; i++)
                Assert.Equal(1, buffer[i]);
            for (int i = length; i < newLength; i++)
                Assert.Equal(0, buffer[i]);

            for (int i = length; i < newLength; i++)
                buffer[i] = 2;

            var returnLengthToClear = newLength - 2;
            ArrayPoolHelper<byte>.Return(buffer, returnLengthToClear);
            for (int i = 0; i < returnLengthToClear; i++)
                Assert.Equal(0, buffer[i]);
            for (int i = returnLengthToClear; i < newLength; i++)
                Assert.Equal(2, buffer[i]);
        }
    }
}
