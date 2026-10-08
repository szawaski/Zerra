// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.IO.Compression;
using System.Security.Cryptography;
using Xunit;
using Zerra.Buffers;
using Zerra.Collections;
using Zerra.Compression;
using Zerra.Encryption;

namespace Zerra.Test.Collections
{
    public class CollectionEdgeTests
    {
        private static List<object?> Enumerate(IEnumerable items)
        {
            var list = new List<object?>();
            var enumerator = items.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);
            return list;
        }

        [Fact]
        public void NonGenericEnumerators()
        {
            Assert.Equal([1, 2], Enumerate(new ConcurrentList<int>() { 1, 2 }));
            Assert.Equal([1, 2], Enumerate(new ConcurrentReadWriteList<int>() { 1, 2 }));
            Assert.Equal([1], Enumerate(new ConcurrentHashSet<int>() { 1 }));
            Assert.Equal([1], Enumerate(new ConcurrentReadWriteHashSet<int>() { 1 }));
            Assert.Single(Enumerate(new ConcurrentFactoryDictionary<int, int>() { [1] = 2 }));
            Assert.Equal([1, 2, 3], Enumerate(new ReadOnlyStack<int>([1, 2, 3])));
        }

        [Fact]
        public void SortedDictionaries_NonGenericEnumeratorAndNullKeys()
        {
            foreach (var dictionary in new IDictionary<string, int>[] { new ConcurrentSortedDictionary<string, int>(), new ConcurrentSortedReadWriteDictionary<string, int>() })
            {
                dictionary["a"] = 1;
                dictionary["b"] = 2;

                var enumerator = ((IDictionary)dictionary).GetEnumerator();
                Assert.True(enumerator.MoveNext());
                Assert.Equal(new KeyValuePair<string, int>("a", 1), enumerator.Current);
                Assert.Equal("a", enumerator.Key);
                Assert.Equal(1, enumerator.Value);
                Assert.Equal("a", enumerator.Entry.Key);
                enumerator.Reset();
                Assert.True(enumerator.MoveNext());
                Assert.Equal("a", enumerator.Key);

                Assert.False(dictionary.ContainsKey(null!));
                Assert.False(dictionary.Remove("missing"));
            }
        }

        [Fact]
        public void ReadOnlyStack_AsCollection()
        {
            ICollection stack = new ReadOnlyStack<int>([1, 2, 3]);
            Assert.False(stack.IsSynchronized);
            Assert.NotNull(stack.SyncRoot);
            Assert.Same(stack.SyncRoot, stack.SyncRoot);
            var array = new int[4];
            stack.CopyTo(array, 1);
            Assert.Equal([0, 1, 2, 3], array);
            _ = Assert.Throws<ArgumentException>(() => stack.CopyTo(new int[2], 0));
        }

        [Fact]
        public async Task AsyncConcurrentQueue_CanceledWaiters()
        {
            var queue = new AsyncConcurrentQueue<int>();

            //a waiter canceled before an item arrives is skipped
            using (var canceller = new CancellationTokenSource())
            {
                var waiting = queue.DequeueAsync(canceller.Token);
                canceller.Cancel();
                _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
            }
            queue.Enqueue(5);
            Assert.Equal(1, queue.Count);
            Assert.Equal(5, await queue.DequeueAsync(TestContext.Current.CancellationToken));

            //an enumerator with a canceled token ends
            await using var enumerator = queue.GetAsyncEnumerator(new CancellationToken(true));
            Assert.False(await enumerator.MoveNextAsync());
        }

        [Fact]
        public void ArrayPoolHelper_Grow()
        {
            Assert.Equal(1, ArrayPoolHelper<byte>.ElementSize);
            Assert.Equal(4, ArrayPoolHelper<int>.ElementSize);

            byte[] buffer = null!;
            _ = Assert.Throws<ArgumentNullException>(() => ArrayPoolHelper<byte>.Grow(ref buffer, 10));

            var small = new byte[8];
            var same = small;
            ArrayPoolHelper<byte>.Grow(ref small, 4);
            Assert.Same(same, small);

            _ = Assert.Throws<OverflowException>(() => ArrayPoolHelper<byte>.Grow(ref small, ArrayPoolHelper<byte>.MaxArraySize + 1));
        }

        [Fact]
        public void Password_RandomNumberRange()
        {
            using var rng = RandomNumberGenerator.Create();
            Assert.Equal(3, Password.GetRandomNumber(rng, 3, 3));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => Password.GetRandomNumber(rng, 5, 1));
        }

        [Fact]
        public void ZerraCompressor_UnknownAlgorithm_Throws()
        {
            _ = Assert.Throws<NotSupportedException>(() => new ZerraCompressor((CompressionAlgorithmType)99, CompressionLevel.Fastest));
        }
    }
}
