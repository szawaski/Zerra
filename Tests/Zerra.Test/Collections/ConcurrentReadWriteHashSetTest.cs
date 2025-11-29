// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class ConcurrentReadWriteHashSetTest
    {
        [Fact]
        public void Constructor_Default()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            Assert.Empty(set);
        }

        [Fact]
        public void Count_Property()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            Assert.Empty(set);
            _ = set.Add(1);
            _ = Assert.Single(set);
            _ = set.Add(2);
            Assert.Equal(2, set.Count);
        }

        [Fact]
        public void IsReadOnly_Property()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            Assert.False(set.IsReadOnly);
        }

        [Fact]
        public void Add_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            Assert.True(set.Add(1));
            Assert.False(set.Add(1));
            Assert.True(set.Add(2));
        }

        [Fact]
        public void Clear_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            Assert.Equal(2, set.Count);
            set.Clear();
            Assert.Empty(set);
        }

        [Fact]
        public void Contains_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            Assert.True(set.Contains(1));
            Assert.False(set.Contains(2));
        }

        [Fact]
        public void CopyTo_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            var array = new int[2];
            set.CopyTo(array);
            var sorted = array.OrderBy(x => x).ToArray();
            Assert.Equal(1, sorted[0]);
            Assert.Equal(2, sorted[1]);
        }

        [Fact]
        public void CopyTo_WithIndex()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            var array = new int[3];
            set.CopyTo(array, 1);
            Assert.Equal(0, array[0]);
            var sorted = array.Skip(1).OrderBy(x => x).ToArray();
            Assert.Equal(1, sorted[0]);
            Assert.Equal(2, sorted[1]);
        }

        [Fact]
        public void CopyTo_WithCount()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            var array = new int[5];
            set.CopyTo(array, 1, 2);
            Assert.Equal(0, array[0]);
            Assert.Equal(0, array[3]);
            Assert.Equal(0, array[4]);
        }

        [Fact]
        public void Remove_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            Assert.True(set.Remove(1));
            Assert.False(set.Contains(1));
            Assert.False(set.Remove(1));
        }

        [Fact]
        public void RemoveWhere_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            var count = set.RemoveWhere(x => x > 1);
            Assert.Equal(2, count);
            Assert.True(set.Contains(1));
            Assert.False(set.Contains(2));
        }

        [Fact]
        public void ExceptWith_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            set.ExceptWith(new[] { 2, 3, 4 });
            _ = Assert.Single(set);
            Assert.Contains(1, set);
        }

        [Fact]
        public void IntersectWith_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            set.IntersectWith(new[] { 2, 3, 4 });
            Assert.Equal(2, set.Count);
            Assert.Contains(2, set);
            Assert.Contains(3, set);
        }

        [Fact]
        public void UnionWith_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            set.UnionWith(new[] { 2, 3, 4 });
            Assert.Equal(4, set.Count);
        }

        [Fact]
        public void SymmetricExceptWith_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            set.SymmetricExceptWith(new[] { 2, 3, 4 });
            Assert.Equal(2, set.Count);
            Assert.Contains(1, set);
            Assert.Contains(4, set);
        }

        [Fact]
        public void IsSubsetOf_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            Assert.True(set.IsSubsetOf(new[] { 1, 2, 3 }));
            Assert.False(set.IsSubsetOf(new[] { 1, 3 }));
        }

        [Fact]
        public void IsSupersetOf_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            Assert.True(set.IsSupersetOf(new[] { 1, 2 }));
            Assert.False(set.IsSupersetOf(new[] { 1, 4 }));
        }

        [Fact]
        public void IsProperSubsetOf_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            Assert.True(set.IsProperSubsetOf(new[] { 1, 2, 3 }));
            Assert.False(set.IsProperSubsetOf(new[] { 1, 2 }));
        }

        [Fact]
        public void IsProperSupersetOf_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            Assert.True(set.IsProperSupersetOf(new[] { 1, 2 }));
            Assert.False(set.IsProperSupersetOf(new[] { 1, 2, 3 }));
        }

        [Fact]
        public void Overlaps_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            Assert.True(set.Overlaps(new[] { 2, 4 }));
            Assert.False(set.Overlaps(new[] { 4, 5 }));
        }

        [Fact]
        public void SetEquals_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            Assert.True(set.SetEquals(new[] { 1, 2, 3 }));
            Assert.True(set.SetEquals(new[] { 3, 2, 1 }));
            Assert.False(set.SetEquals(new[] { 1, 2 }));
        }

        [Fact]
        public void TrimExcess_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Remove(1);
            _ = set.Remove(2);
            set.TrimExcess();
            Assert.Empty(set);
        }

        [Fact]
        public void GetEnumerator_Method()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            _ = set.Add(2);
            _ = set.Add(3);
            var items = set.ToList();
            Assert.Equal(3, items.Count);
        }

        [Fact]
        public void Dispose_Method()
        {
            var set = new ConcurrentReadWriteHashSet<int>();
            _ = set.Add(1);
            set.Dispose();
            _ = Assert.Throws<ObjectDisposedException>(() => set.Add(2));
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentReadsAndWrites()
        {
            using var set = new ConcurrentReadWriteHashSet<int>();
            for (int i = 0; i < 100; i++)
                _ = set.Add(i);

            var readerCount = 0;
            var tasks = new Task[20];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var count = set.Count;
                    _ = System.Threading.Interlocked.Increment(ref readerCount);
                });
            }

            for (int i = 10; i < 20; i++)
            {
                int index = i - 10;
                tasks[i] = Task.Run(() =>
                {
                    _ = set.Add(1000 + index);
                });
            }

            await Task.WhenAll(tasks);
            Assert.Equal(110, set.Count);
            Assert.Equal(10, readerCount);
        }
    }
}
