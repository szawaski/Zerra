// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Serialization.Bytes;
using Zerra.Test.Helpers.Models;

namespace Zerra.Test.Serialization
{
    //lengths and counts come from the data, a few bytes claiming a huge one must not allocate it
    public class ByteSerializerUntrustedLengthTests
    {
        private const int timeout = 30000;
        private const long allowedAllocation = 16 * 1024 * 1024;

        //replaces the first little endian occurrence of a count, the counts used are chosen to only appear where the count is written
        private static byte[] ReplaceCount(byte[] bytes, int oldCount, int newCount)
        {
            for (var i = 0; i <= bytes.Length - 4; i++)
            {
                if (BitConverter.ToInt32(bytes, i) == oldCount)
                {
                    var copy = (byte[])bytes.Clone();
                    BitConverter.GetBytes(newCount).CopyTo(copy, i);
                    return copy;
                }
            }
            throw new InvalidOperationException("Count not found");
        }

        //the false length followed by more data, so the reader keeps going instead of seeing the stream end early
        private static MemoryStream WithMoreData(byte[] bytes, int moreData)
        {
            var stream = new MemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Write(new byte[moreData], 0, moreData);
            stream.Position = 0;
            return stream;
        }

        private static List<SimpleModel> Models(int count)
        {
            var list = new List<SimpleModel>(count);
            for (var i = 0; i < count; i++)
                list.Add(new SimpleModel() { Value1 = 1000 + i, Value2 = "x" });
            return list;
        }

#if !NETSTANDARD2_0
        private static long AllocatedBy(Action action)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            try
            {
                action();
            }
            catch (EndOfStreamException) { }
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        [Fact]
        public void Stream_ClaimedStringLength_GrowsWithTheData()
        {
            var bytes = ReplaceCount(ByteSerializer.Serialize("abcdefg"), 7, 200_000_000);
            var stream = WithMoreData(bytes, 2 * 1024 * 1024);
            var allocated = AllocatedBy(() => ByteSerializer.Deserialize<string>(stream));
            Assert.True(allocated < allowedAllocation, $"Allocated {allocated}");
        }

        [Fact]
        public void Stream_ClaimedArrayLength_GrowsWithTheData()
        {
            var bytes = ReplaceCount(ByteSerializer.Serialize(new int[] { 1, 2, 3, 4, 5, 6, 7 }), 7, 50_000_000);
            var stream = WithMoreData(bytes, 2 * 1024 * 1024);
            var allocated = AllocatedBy(() => ByteSerializer.Deserialize<int[]>(stream));
            Assert.True(allocated < allowedAllocation, $"Allocated {allocated}");
        }

        [Fact]
        public void Bytes_ClaimedCollectionCount_IsNotPresized()
        {
            var list = ReplaceCount(ByteSerializer.Serialize(Models(7)), 7, 100_000_000);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<List<SimpleModel>>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<SimpleModel[]>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<IList<SimpleModel>>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<ICollection<SimpleModel>>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<IReadOnlyCollection<SimpleModel>>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<IEnumerable<SimpleModel>>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<HashSet<SimpleModel>>(list)) < allowedAllocation);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<ISet<SimpleModel>>(list)) < allowedAllocation);
        }

        [Fact]
        public void Stream_ClaimedCollectionCount_IsNotPresized()
        {
            var list = ReplaceCount(ByteSerializer.Serialize(Models(7)), 7, 100_000_000);
            //each zero byte reads as a null item, so the collection grows with the data sent
            var listStream = WithMoreData(list, 256 * 1024);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<List<SimpleModel>>(listStream)) < allowedAllocation);
            var arrayStream = WithMoreData(list, 256 * 1024);
            Assert.True(AllocatedBy(() => ByteSerializer.Deserialize<SimpleModel[]>(arrayStream)) < allowedAllocation);
        }
#endif

        [Fact]
        public void ClaimedLengthOverflowingTheSize_Throws()
        {
            var array = ReplaceCount(ByteSerializer.Serialize(new int[] { 1, 2, 3, 4, 5, 6, 7 }), 7, 0x40000001);
            _ = Assert.Throws<InvalidDataException>(() => ByteSerializer.Deserialize<int[]>(array));

            var negative = ReplaceCount(ByteSerializer.Serialize(new long[] { 1, 2, 3, 4, 5, 6, 7 }), 7, -2);
            _ = Assert.Throws<InvalidDataException>(() => ByteSerializer.Deserialize<long[]>(negative));

            var bytes = ReplaceCount(ByteSerializer.Serialize(new byte[] { 1, 2, 3, 4, 5, 6, 7 }), 7, int.MaxValue);
            _ = Assert.Throws<InvalidDataException>(() => ByteSerializer.Deserialize<byte[]>(bytes));
        }

        [Fact(Timeout = timeout)]
        public async Task Stream_ClaimedStringLengthNearIntMax_Throws()
        {
            var bytes = ReplaceCount(ByteSerializer.Serialize("abcdefg"), 7, int.MaxValue - 1);
            _ = await Assert.ThrowsAsync<InvalidDataException>(async () => await ByteSerializer.DeserializeAsync<string>(new MemoryStream(bytes), cancellationToken: TestContext.Current.CancellationToken));
            _ = Assert.Throws<InvalidDataException>(() => ByteSerializer.Deserialize<string>(new MemoryStream(bytes)));
        }

        //real values past the thresholds read the same, from a stream they arrive in pieces and grow as they do
        [Fact]
        public void ValuesPastTheThresholds_RoundTrip()
        {
            var text = new string('a', 3 * 1024 * 1024) + "é";
            Assert.Equal(text, ByteSerializer.Deserialize<string>(ByteSerializer.Serialize(text)));
            Assert.Equal(text, ByteSerializer.Deserialize<string>(new MemoryStream(ByteSerializer.Serialize(text))));

            var numbers = Enumerable.Range(0, 1_000_000).ToArray();
            Assert.Equal(numbers, ByteSerializer.Deserialize<int[]>(new MemoryStream(ByteSerializer.Serialize(numbers))));

            var models = Models(200_000);
            var bytes = ByteSerializer.Serialize(models);
            static int[] Values(IEnumerable<SimpleModel>? models) => models!.Select(x => x.Value1).ToArray();
            var expected = Values(models);

            Assert.Equal(expected, Values(ByteSerializer.Deserialize<List<SimpleModel>>(new MemoryStream(bytes))));
            Assert.Equal(expected, Values(ByteSerializer.Deserialize<SimpleModel[]>(new MemoryStream(bytes))));
            Assert.Equal(expected, Values(ByteSerializer.Deserialize<IList<SimpleModel>>(new MemoryStream(bytes))));
            Assert.Equal(expected, Values(ByteSerializer.Deserialize<ICollection<SimpleModel>>(new MemoryStream(bytes))));
            Assert.Equal(expected, Values(ByteSerializer.Deserialize<IReadOnlyCollection<SimpleModel>>(new MemoryStream(bytes))));
            Assert.Equal(expected, Values(ByteSerializer.Deserialize<IEnumerable<SimpleModel>>(new MemoryStream(bytes))));
            Assert.Equal(expected.Length, ByteSerializer.Deserialize<HashSet<SimpleModel>>(new MemoryStream(bytes))!.Count);
            Assert.Equal(expected, Values(ByteSerializer.Deserialize<SimpleModel[]>(bytes)));

            var resized = ByteSerializer.Deserialize<SimpleModel[]>(new MemoryStream(bytes))!;
            Assert.Equal(models.Count, resized.Length);
        }
    }
}
