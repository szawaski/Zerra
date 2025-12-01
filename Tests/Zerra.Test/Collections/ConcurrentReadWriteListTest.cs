// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

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
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(1, 2));
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
                });
            }

            for (int i = 10; i < 20; i++)
            {
                int index = i - 10;
                tasks[i] = Task.Run(() =>
                {
                    list.Add(1000 + index);
                });
            }

            await Task.WhenAll(tasks);
            Assert.Equal(110, list.Count);
            Assert.Equal(10, readerCount);
        }
    }
}
