// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class ConcurrentSortedDictionaryTest
    {
        [Fact]
        public void Constructor_Default()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            Assert.Empty(dict);
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void Constructor_WithDictionary()
        {
            var collection = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            var dict = new ConcurrentSortedDictionary<string, int>(collection);
            Assert.Equal(2, dict.Count);
            Assert.Equal(1, dict["a"]);
            Assert.Equal(2, dict["b"]);
        }

        [Fact]
        public void Constructor_WithComparer()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new ConcurrentSortedDictionary<string, int>(comparer);
            Assert.Empty(dict);
        }

        [Fact]
        public void Constructor_WithDictionaryAndComparer()
        {
            var collection = new Dictionary<string, int> { { "a", 1 } };
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new ConcurrentSortedDictionary<string, int>(collection, comparer);
            _ = Assert.Single(dict);
        }

        [Fact]
        public void IsEmpty_WhenEmpty()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenNotEmpty()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.False(dict.IsEmpty);
        }

        [Fact]
        public void Count_Property()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            Assert.Empty(dict);
            _ = dict.TryAdd("a", 1);
            _ = Assert.Single(dict);
            _ = dict.TryAdd("b", 2);
            Assert.Equal(2, dict.Count);
        }

        [Fact]
        public void Keys_Property()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
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
            var dict = new ConcurrentSortedDictionary<string, int>();
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
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void Indexer_Get_KeyNotFound()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = Assert.Throws<KeyNotFoundException>(() => dict["a"]);
        }

        [Fact]
        public void Indexer_Set()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            dict["a"] = 1;
            Assert.Equal(1, dict["a"]);
            dict["a"] = 2;
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void Clear_Method()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
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
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.ContainsKey("a"));
            Assert.False(dict.ContainsKey("b"));
        }

        [Fact]
        public void GetEnumerator_Method()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
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
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            var result = dict.GetOrAdd("a", 1);
            Assert.Equal(1, result);
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void GetOrAdd_WithFactory()
        {
            var dict = new ConcurrentSortedDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", (key) => key.ToUpper());
            Assert.Equal("A", result1);
            var result2 = dict.GetOrAdd("a", (key) => key.ToLower());
            Assert.Equal("A", result2);
        }

        [Fact]
        public void ToArray_Method()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
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
            var dict = new ConcurrentSortedDictionary<string, int>();
            Assert.True(dict.TryAdd("a", 1));
            Assert.Equal(1, dict["a"]);
            Assert.False(dict.TryAdd("a", 2));
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void TryGetValue_Method()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryGetValue("a", out var value));
            Assert.Equal(1, value);
            Assert.False(dict.TryGetValue("b", out _));
        }

        [Fact]
        public void TryUpdate_Method()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryUpdate("a", 2, 1));
            Assert.Equal(2, dict["a"]);
            Assert.False(dict.TryUpdate("a", 3, 1));
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void TryRemove_Method()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryRemove("a", out var value));
            Assert.Equal(1, value);
            Assert.False(dict.ContainsKey("a"));
            Assert.False(dict.TryRemove("b", out _));
        }

        [Fact]
        public void AddOrUpdate_WithFactories()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            var result1 = dict.AddOrUpdate("a", k => 1, (k, v) => v + 1);
            Assert.Equal(1, result1);
            var result2 = dict.AddOrUpdate("a", k => 2, (k, v) => v + 10);
            Assert.Equal(11, result2);
            Assert.Equal(11, dict["a"]);
        }

        [Fact]
        public void AddOrUpdate_WithValue()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            var result1 = dict.AddOrUpdate("a", 1, (k, v) => v + 1);
            Assert.Equal(1, result1);
            var result2 = dict.AddOrUpdate("a", 2, (k, v) => v + 10);
            Assert.Equal(11, result2);
            Assert.Equal(11, dict["a"]);
        }

        [Fact]
        public void SortedOrder()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            _ = dict.TryAdd("zebra", 1);
            _ = dict.TryAdd("apple", 2);
            _ = dict.TryAdd("banana", 3);

            var keys = dict.Keys.ToList();
            Assert.Equal("apple", keys[0]);
            Assert.Equal("banana", keys[1]);
            Assert.Equal("zebra", keys[2]);
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentOperations()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            var tasks = new Task[10];

            for (int i = 0; i < 10; i++)
            {
                int value = i;
                tasks[i] = Task.Run(() =>
                {
                    _ = dict.TryAdd($"key{value}", value);
                    _ = dict.TryAdd($"key{value + 100}", value + 100);
                }, TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(tasks);
            Assert.Equal(20, dict.Count);
        }

        [Fact]
        public void GenericInterfaces()
        {
            var dict = new ConcurrentSortedDictionary<string, int?>();
            IDictionary<string, int?> dictionary = dict;
            var collection = (ICollection<KeyValuePair<string, int?>>)dict;

            dictionary.Add("a", 1);
            _ = Assert.Throws<ArgumentException>(() => dictionary.Add("a", 2));
            _ = Assert.Throws<ArgumentException>(() => collection.Add(new("a", 3)));
            Assert.Equal(1, dictionary["a"]);

            Assert.True(collection.Contains(new("a", 1)));
            Assert.False(collection.Contains(new("a", 2)));
            Assert.False(collection.Remove(new("a", 2)));
            Assert.True(dictionary.ContainsKey("a"));
            Assert.True(collection.Remove(new("a", 1)));
            Assert.False(dictionary.ContainsKey("a"));
            Assert.False(collection.IsReadOnly);

            collection.Add(new("b", 2));
            var array = new KeyValuePair<string, int?>[2];
            collection.CopyTo(array, 1);
            Assert.Equal(new KeyValuePair<string, int?>("b", 2), array[1]);
            _ = Assert.ThrowsAny<ArgumentException>(() => collection.CopyTo(new KeyValuePair<string, int?>[0], 0));

            var readOnly = (IReadOnlyDictionary<string, int?>)dict;
            Assert.Equal(["b"], readOnly.Keys);
            Assert.Equal([2], readOnly.Values);

            Assert.True(dictionary.Remove("b"));
            Assert.False(dictionary.Remove("b"));
        }

        [Fact]
        public void NonGenericInterfaces()
        {
            var dict = new ConcurrentSortedDictionary<string, int?>();
            IDictionary dictionary = dict;

            dictionary.Add("a", null);
            Assert.True(dictionary.Contains("a"));
            Assert.Null(dictionary["a"]);
            _ = Assert.Throws<ArgumentException>(() => dictionary.Add("a", 1));
            _ = Assert.Throws<ArgumentException>(() => dictionary.Add(5, 1));
            _ = Assert.Throws<ArgumentException>(() => dictionary.Add("b", "text"));

            Assert.Null(dictionary["missing"]);
            Assert.Null(dictionary[5]);
            Assert.False(dictionary.Contains("missing"));
            Assert.False(dictionary.Contains(5));

            dictionary["b"] = 2;
            Assert.Equal(2, dictionary["b"]);
            _ = Assert.Throws<ArgumentException>(() => dictionary["b"] = "text");

            dictionary.Remove("missing");
            dictionary.Remove(5);
            Assert.Equal(2, dictionary.Count);

            Assert.Equal(2, dictionary.Keys.Count);
            Assert.Equal(2, dictionary.Values.Count);
            var keys = new List<object>();
            var enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Assert.Equal(enumerator.Key, enumerator.Entry.Key);
                keys.Add(enumerator.Key);
            }
            Assert.Equal(["a", "b"], keys.OrderBy(x => x));

            var array = new KeyValuePair<string, int?>[2];
            ((ICollection)dictionary).CopyTo(array, 0);
            Assert.Equal(["a", "b"], array.Select(x => x.Key).OrderBy(x => x));

            Assert.False(dictionary.IsFixedSize);
            Assert.False(dictionary.IsReadOnly);
            Assert.False(((ICollection)dictionary).IsSynchronized);
            _ = Assert.Throws<NotSupportedException>(() => ((ICollection)dictionary).SyncRoot);

            dictionary.Remove("a");
            Assert.False(dictionary.Contains("a"));
            dictionary.Clear();
            Assert.Empty(dictionary);
        }

        [Fact]
        public void TryUpdate_ComparesNullsLikeConcurrentDictionary()
        {
            var dict = new ConcurrentSortedDictionary<string, string?>();
            dict["a"] = null;

            Assert.False(dict.TryUpdate("a", "x", "b"));
            Assert.Null(dict["a"]);
            Assert.True(dict.TryUpdate("a", "x", null));
            Assert.Equal("x", dict["a"]);
            Assert.False(dict.TryUpdate("a", "y", null));
            Assert.Equal("x", dict["a"]);
            Assert.True(dict.TryUpdate("a", null, "x"));
            Assert.Null(dict["a"]);
            Assert.False(dict.TryUpdate("missing", "x", null));
        }

        [Fact]
        public void TryRemove_ContainsKey_GetOrAdd_Enumerate()
        {
            var dict = new ConcurrentSortedDictionary<string, int>();
            Assert.False(dict.TryRemove("a", out _));
            Assert.Equal(1, dict.GetOrAdd("a", 1));
            Assert.Equal(1, dict.GetOrAdd("a", 2));
            Assert.Equal(1, dict.GetOrAdd("a", _ => 3));
            Assert.True(dict.ContainsKey("a"));
            Assert.Single(((IEnumerable)dict).Cast<object>());
            Assert.True(dict.TryRemove("a", out var removed));
            Assert.Equal(1, removed);
            Assert.False(dict.ContainsKey("a"));
        }
    }
}
