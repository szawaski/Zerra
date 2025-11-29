// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class ConcurrentSortedReadWriteDictionaryTest
    {
        [Fact]
        public void Constructor_Default()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            Assert.Empty(dict);
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void Constructor_WithDictionary()
        {
            var collection = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>(collection);
            Assert.Equal(2, dict.Count);
            Assert.Equal(1, dict["a"]);
            Assert.Equal(2, dict["b"]);
        }

        [Fact]
        public void Constructor_WithComparer()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>(comparer);
            Assert.Empty(dict);
        }

        [Fact]
        public void Constructor_WithDictionaryAndComparer()
        {
            var collection = new Dictionary<string, int> { { "a", 1 } };
            var comparer = StringComparer.OrdinalIgnoreCase;
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>(collection, comparer);
            _ = Assert.Single(dict);
        }

        [Fact]
        public void IsEmpty_WhenEmpty()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenNotEmpty()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.False(dict.IsEmpty);
        }

        [Fact]
        public void Count_Property()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            Assert.Empty(dict);
            _ = dict.TryAdd("a", 1);
            _ = Assert.Single(dict);
            _ = dict.TryAdd("b", 2);
            Assert.Equal(2, dict.Count);
        }

        [Fact]
        public void Keys_Property()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("b", 2);
            _ = dict.TryAdd("a", 1);
            var keys = dict.Keys.ToList();
            Assert.Equal(2, keys.Count);
            Assert.Equal("a", keys[0]);
            Assert.Equal("b", keys[1]);
        }

        [Fact]
        public void Values_Property()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            var values = dict.Values.ToList();
            Assert.Equal(2, values.Count);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
        }

        [Fact]
        public void Indexer_Get()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void Indexer_Get_KeyNotFound()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = Assert.Throws<KeyNotFoundException>(() => dict["a"]);
        }

        [Fact]
        public void Indexer_Set()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            dict["a"] = 1;
            Assert.Equal(1, dict["a"]);
            dict["a"] = 2;
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void Clear_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            Assert.Equal(2, dict.Count);
            dict.Clear();
            Assert.Empty(dict);
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void ContainsKey_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.ContainsKey("a"));
            Assert.False(dict.ContainsKey("b"));
        }

        [Fact]
        public void GetEnumerator_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("b", 2);
            _ = dict.TryAdd("a", 1);
            var items = dict.ToList();
            Assert.Equal(2, items.Count);
            Assert.Equal("a", items[0].Key);
            Assert.Equal(1, items[0].Value);
            Assert.Equal("b", items[1].Key);
            Assert.Equal(2, items[1].Value);
        }

        [Fact]
        public void GetOrAdd_WithValue()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            var result = dict.GetOrAdd("a", 1);
            Assert.Equal(1, result);
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void GetOrAdd_WithFactory()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", (key) => key.ToUpper());
            Assert.Equal("A", result1);
            var result2 = dict.GetOrAdd("a", (key) => key.ToLower());
            Assert.Equal("A", result2);
        }

        [Fact]
        public void ToArray_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            var array = dict.ToArray();
            Assert.Equal(2, array.Length);
            Assert.Equal("a", array[0].Key);
            Assert.Equal(1, array[0].Value);
            Assert.Equal("b", array[1].Key);
            Assert.Equal(2, array[1].Value);
        }

        [Fact]
        public void TryAdd_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            Assert.True(dict.TryAdd("a", 1));
            Assert.Equal(1, dict["a"]);
            Assert.False(dict.TryAdd("a", 2));
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void TryGetValue_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryGetValue("a", out var value));
            Assert.Equal(1, value);
            Assert.False(dict.TryGetValue("b", out _));
        }

        [Fact]
        public void TryUpdate_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryUpdate("a", 2, 1));
            Assert.Equal(2, dict["a"]);
            Assert.False(dict.TryUpdate("a", 3, 1));
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void TryRemove_Method()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryRemove("a", out var value));
            Assert.Equal(1, value);
            Assert.False(dict.ContainsKey("a"));
            Assert.False(dict.TryRemove("b", out _));
        }

        [Fact]
        public void AddOrUpdate_WithFactories()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            var result1 = dict.AddOrUpdate("a", k => 1, (k, v) => v + 1);
            Assert.Equal(1, result1);
            var result2 = dict.AddOrUpdate("a", k => 2, (k, v) => v + 10);
            Assert.Equal(11, result2);
            Assert.Equal(11, dict["a"]);
        }

        [Fact]
        public void AddOrUpdate_WithValue()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            var result1 = dict.AddOrUpdate("a", 1, (k, v) => v + 1);
            Assert.Equal(1, result1);
            var result2 = dict.AddOrUpdate("a", 2, (k, v) => v + 10);
            Assert.Equal(11, result2);
            Assert.Equal(11, dict["a"]);
        }

        [Fact]
        public void SortedOrder()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("zebra", 1);
            _ = dict.TryAdd("apple", 2);
            _ = dict.TryAdd("banana", 3);

            var keys = dict.Keys.ToList();
            Assert.Equal("apple", keys[0]);
            Assert.Equal("banana", keys[1]);
            Assert.Equal("zebra", keys[2]);
        }

        [Fact]
        public void Dispose_Method()
        {
            var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            dict.Dispose();
            _ = Assert.Throws<ObjectDisposedException>(() => dict.TryAdd("b", 2));
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentReadsAndWrites()
        {
            using var dict = new ConcurrentSortedReadWriteDictionary<string, int>();
            for (int i = 0; i < 50; i++)
                _ = dict.TryAdd($"key{i}", i);

            var readCount = 0;
            var tasks = new Task[20];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var count = dict.Count;
                    _ = System.Threading.Interlocked.Increment(ref readCount);
                });
            }

            for (int i = 10; i < 20; i++)
            {
                int index = i - 10;
                tasks[i] = Task.Run(() =>
                {
                    _ = dict.TryAdd($"new{index}", 1000 + index);
                });
            }

            await Task.WhenAll(tasks);
            Assert.Equal(60, dict.Count);
            Assert.Equal(10, readCount);
        }
    }
}
