// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class ConcurrentReadWriteListTest
    {
        [Fact]
        public void Constructor_Default()
        {
            using var list = new ConcurrentReadWriteList<int>();
            Assert.Empty(list);
        }

        [Fact]
        public void Count_Property()
        {
            using var list = new ConcurrentReadWriteList<int>();
            Assert.Empty(list);
            list.Add(1);
            _ = Assert.Single(list);
            list.Add(2);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public void IsReadOnly_Property()
        {
            using var list = new ConcurrentReadWriteList<int>();
            Assert.False(list.IsReadOnly);
        }

        [Fact]
        public void Indexer_Get()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(10);
            list.Add(20);
            Assert.Equal(10, list[0]);
            Assert.Equal(20, list[1]);
        }

        [Fact]
        public void Indexer_Set()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(10);
            list[0] = 15;
            Assert.Equal(15, list[0]);
        }

        [Fact]
        public void Indexer_OutOfRange()
        {
            using var list = new ConcurrentReadWriteList<int>();
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list[0]);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list[0] = 1);
        }

        [Fact]
        public void Add_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
        }

        [Fact]
        public void AddRange_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.AddRange(new[] { 1, 2, 3 });
            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);
        }

        [Fact]
        public void Clear_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            list.Clear();
            Assert.Empty(list);
        }

        [Fact]
        public void Contains_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            Assert.Contains(1, list);
            Assert.DoesNotContain(3, list);
        }

        [Fact]
        public void CopyTo_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            var array = new int[2];
            list.CopyTo(array, 0);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
        }

        [Fact]
        public void ToArray_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            var array = list.ToArray();
            Assert.Equal(2, array.Length);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
        }

        [Fact]
        public void IndexOf_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            Assert.Equal(0, list.IndexOf(1));
            Assert.Equal(1, list.IndexOf(2));
            Assert.Equal(2, list.IndexOf(3));
            Assert.Equal(-1, list.IndexOf(4));
        }

        [Fact]
        public void Insert_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(3);
            list.Insert(1, 2);
            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);
        }

        [Fact]
        public void Insert_OutOfRange()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 2));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(2, 2));
            list.Insert(1, 2);
            Assert.Equal([1, 2], list);
        }

        [Fact]
        public void Remove_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            Assert.True(list.Remove(2));
            Assert.Equal(2, list.Count);
            Assert.False(list.Remove(4));
        }

        [Fact]
        public void RemoveAt_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.RemoveAt(1);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(3, list[1]);
        }

        [Fact]
        public void RemoveAt_OutOfRange()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(1));
        }

        [Fact]
        public void GetEnumerator_Method()
        {
            using var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);
            var items = list.ToList();
            Assert.Equal(3, items.Count);
            Assert.Equal(1, items[0]);
            Assert.Equal(2, items[1]);
            Assert.Equal(3, items[2]);
        }

        [Fact]
        public void Dispose_Method()
        {
            var list = new ConcurrentReadWriteList<int>();
            list.Add(1);
            list.Dispose();
            _ = Assert.Throws<ObjectDisposedException>(() => list.Add(2));
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentReadsAndWrites()
        {
            using var list = new ConcurrentReadWriteList<int>();
            for (int i = 0; i < 100; i++)
                list.Add(i);

            var readerCount = 0;
            var tasks = new Task[20];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var count = list.Count;
                    _ = System.Threading.Interlocked.Increment(ref readerCount);
                }, TestContext.Current.CancellationToken);
            }

            for (int i = 10; i < 20; i++)
            {
                int index = i - 10;
                tasks[i] = Task.Run(() =>
                {
                    list.Add(1000 + index);
                }, TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(tasks);
            Assert.Equal(110, list.Count);
            Assert.Equal(10, readerCount);
        }

        [Fact]
        public void GenericInterfaces()
        {
            using var concurrentList = new ConcurrentReadWriteList<int?>();
            IList<int?> list = concurrentList;

            list.CopyTo([], 0);

            list.Add(1);
            list.Insert(1, 3);
            list.Insert(1, 2);
            Assert.Equal([1, 2, 3], list);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(5, 4));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list[3]);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 0);
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(3));

            var array = new int?[4];
            list.CopyTo(array, 1);
            Assert.Equal([null, 1, 2, 3], array);
            _ = Assert.ThrowsAny<ArgumentException>(() => list.CopyTo(new int?[2], 0));
        }

        [Fact]
        public void NonGenericInterfaces()
        {
            using var concurrentList = new ConcurrentReadWriteList<int?>();
            IList list = concurrentList;

            Assert.Equal(0, list.Add(null));
            Assert.Equal(1, list.Add(2));
            Assert.True(list.Contains(null));
            Assert.False(list.Contains("text"));
            Assert.Equal(1, list.IndexOf(2));
            Assert.Equal(-1, list.IndexOf("text"));
            list.Remove("text");
            Assert.Equal(2, list.Count);

            _ = Assert.Throws<ArgumentException>(() => list.Add("text"));
            _ = Assert.Throws<ArgumentException>(() => list[0] = "text");
            _ = Assert.Throws<ArgumentException>(() => list.Insert(0, "text"));

            list[1] = null;
            Assert.Null(list[1]);
            list.Insert(2, 3);
            list.Remove(null);
            Assert.Equal([null, 3], list.Cast<int?>());

            var array = new object[2];
            list.CopyTo(array, 0);
            Assert.Equal([null, 3], array);

            Assert.False(list.IsFixedSize);
            Assert.False(list.IsReadOnly);
            Assert.False(list.IsSynchronized);
            _ = Assert.Throws<NotSupportedException>(() => list.SyncRoot);

            list.RemoveAt(0);
            list.Clear();
            Assert.Empty(list);
        }

        [Fact]
        public async Task ReleasesLockOnException()
        {
            using var list = new ConcurrentReadWriteList<int>();

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list[0]);
            await AssertCompletes(() => list.Add(1));

            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(5, 1));
            await AssertCompletes(() => list.Add(2));

            _ = Assert.Throws<InvalidOperationException>(() => list.AddRange(ThrowingItems()));
            await AssertCompletes(() => list.Add(3));
        }

        private static IEnumerable<int> ThrowingItems()
        {
            yield return 100;
            throw new InvalidOperationException();
        }

        //a leaked lock blocks writers on other threads forever
        private static Task AssertCompletes(Action action)
            => Task.Run(action).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        [Fact]
        public void Contains_Values()
        {
            var list = new ConcurrentReadWriteList<int>() { 1, 2 };
            //the list's own Contains rather than enumerating it
            var hasTwo = list.Contains(2);
            var hasThree = list.Contains(3);
            Assert.True(hasTwo);
            Assert.False(hasThree);
        }
    }
}
