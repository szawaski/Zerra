// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class ConcurrentFactoryDictionaryTest
    {
        [Fact]
        public void Constructor_Default()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            Assert.Empty(dict);
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void Constructor_WithConcurrencyLevelAndCapacity()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>(4, 16);
            Assert.Empty(dict);
        }

        [Fact]
        public void Constructor_WithCollection()
        {
            var collection = new List<KeyValuePair<string, int>> { new("a", 1), new("b", 2) };
            var dict = new ConcurrentFactoryDictionary<string, int>(collection);
            Assert.Equal(2, dict.Count);
            Assert.Equal(1, dict["a"]);
            Assert.Equal(2, dict["b"]);
        }

        [Fact]
        public void Constructor_WithComparer()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new ConcurrentFactoryDictionary<string, int>(comparer);
            Assert.Empty(dict);
        }

        [Fact]
        public void Constructor_WithCollectionAndComparer()
        {
            var collection = new List<KeyValuePair<string, int>> { new("a", 1) };
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new ConcurrentFactoryDictionary<string, int>(collection, comparer);
            _ = Assert.Single(dict);
        }

        [Fact]
        public void Constructor_WithConcurrencyLevelCollectionAndComparer()
        {
            var collection = new List<KeyValuePair<string, int>> { new("a", 1) };
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new ConcurrentFactoryDictionary<string, int>(4, collection, comparer);
            _ = Assert.Single(dict);
        }

        [Fact]
        public void Constructor_WithConcurrencyLevelCapacityAndComparer()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var dict = new ConcurrentFactoryDictionary<string, int>(4, 16, comparer);
            Assert.Empty(dict);
        }

        [Fact]
        public void IsEmpty_WhenEmpty()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            Assert.True(dict.IsEmpty);
        }

        [Fact]
        public void IsEmpty_WhenNotEmpty()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.False(dict.IsEmpty);
        }

        [Fact]
        public void Count_Property()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            Assert.Empty(dict);
            _ = dict.TryAdd("a", 1);
            _ = Assert.Single(dict);
            _ = dict.TryAdd("b", 2);
            Assert.Equal(2, dict.Count);
        }

        [Fact]
        public void Keys_Property()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            var keys = dict.Keys.OrderBy(k => k).ToList();
            Assert.Equal(2, keys.Count);
            Assert.Equal("a", keys[0]);
            Assert.Equal("b", keys[1]);
        }

        [Fact]
        public void Values_Property()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            var values = dict.Values.OrderBy(v => v).ToList();
            Assert.Equal(2, values.Count);
            Assert.Equal(1, values[0]);
            Assert.Equal(2, values[1]);
        }

        [Fact]
        public void Indexer_Get()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void Indexer_Set()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            dict["a"] = 1;
            Assert.Equal(1, dict["a"]);
            dict["a"] = 2;
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void Clear_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
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
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.ContainsKey("a"));
            Assert.False(dict.ContainsKey("b"));
        }

        [Fact]
        public void GetEnumerator_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            var items = dict.ToList();
            Assert.Equal(2, items.Count);
            var sorted = items.OrderBy(x => x.Key).ToList();
            Assert.Equal("a", sorted[0].Key);
            Assert.Equal(1, sorted[0].Value);
            Assert.Equal("b", sorted[1].Key);
            Assert.Equal(2, sorted[1].Value);
        }

        [Fact]
        public void GetOrAdd_WithValue()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            var result1 = dict.GetOrAdd("a", 1);
            Assert.Equal(1, result1);
            Assert.Equal(1, dict["a"]);
            var result2 = dict.GetOrAdd("a", 2);
            Assert.Equal(1, result2);
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void GetOrAdd_WithFuncNoArg()
        {
            var callCount = 0;
            var dict = new ConcurrentFactoryDictionary<string, int>();
            var result1 = dict.GetOrAdd("a", () => { callCount++; return 1; });
            Assert.Equal(1, result1);
            Assert.Equal(1, callCount);
            var result2 = dict.GetOrAdd("a", () => { callCount++; return 2; });
            Assert.Equal(1, result2);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GetOrAdd_WithFuncTKey()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", (key) => key.ToUpper());
            Assert.Equal("A", result1);
            var result2 = dict.GetOrAdd("a", (key) => key.ToUpper());
            Assert.Equal("A", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg1AndFuncTKeyArg1()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", (key, arg1) => key + arg1);
            Assert.Equal("ax", result1);
            var result2 = dict.GetOrAdd("a", "y", (key, arg1) => key + arg1);
            Assert.Equal("ax", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg1AndFuncArg1()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", (arg1) => arg1.ToUpper());
            Assert.Equal("X", result1);
            var result2 = dict.GetOrAdd("a", "y", (arg1) => arg1.ToUpper());
            Assert.Equal("X", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg2AndFuncTKeyArg1Arg2()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", (key, arg1, arg2) => key + arg1 + arg2);
            Assert.Equal("axy", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", (key, arg1, arg2) => key + arg1 + arg2);
            Assert.Equal("axy", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg2AndFuncArg1Arg2()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", (arg1, arg2) => arg1 + arg2);
            Assert.Equal("xy", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", (arg1, arg2) => arg1 + arg2);
            Assert.Equal("xy", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg3AndFuncTKeyArg1Arg2Arg3()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", "z", (key, arg1, arg2, arg3) => key + arg1 + arg2 + arg3);
            Assert.Equal("axyz", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", "z", (key, arg1, arg2, arg3) => key + arg1 + arg2 + arg3);
            Assert.Equal("axyz", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg3AndFuncArg1Arg2Arg3()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", "z", (arg1, arg2, arg3) => arg1 + arg2 + arg3);
            Assert.Equal("xyz", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", "z", (arg1, arg2, arg3) => arg1 + arg2 + arg3);
            Assert.Equal("xyz", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg4AndFuncTKeyArg1Arg2Arg3Arg4()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", "z", "w", (key, arg1, arg2, arg3, arg4) => key + arg1 + arg2 + arg3 + arg4);
            Assert.Equal("axyzw", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", "z", "w", (key, arg1, arg2, arg3, arg4) => key + arg1 + arg2 + arg3 + arg4);
            Assert.Equal("axyzw", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg4AndFuncArg1Arg2Arg3Arg4()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", "z", "w", (arg1, arg2, arg3, arg4) => arg1 + arg2 + arg3 + arg4);
            Assert.Equal("xyzw", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", "z", "w", (arg1, arg2, arg3, arg4) => arg1 + arg2 + arg3 + arg4);
            Assert.Equal("xyzw", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg5AndFuncTKeyArg1Arg2Arg3Arg4Arg5()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", "z", "w", "v", (key, arg1, arg2, arg3, arg4, arg5) => key + arg1 + arg2 + arg3 + arg4 + arg5);
            Assert.Equal("axyzwv", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", "z", "w", "v", (key, arg1, arg2, arg3, arg4, arg5) => key + arg1 + arg2 + arg3 + arg4 + arg5);
            Assert.Equal("axyzwv", result2);
        }

        [Fact]
        public void GetOrAdd_WithArg5AndFuncArg1Arg2Arg3Arg4Arg5()
        {
            var dict = new ConcurrentFactoryDictionary<string, string>();
            var result1 = dict.GetOrAdd("a", "x", "y", "z", "w", "v", (arg1, arg2, arg3, arg4, arg5) => arg1 + arg2 + arg3 + arg4 + arg5);
            Assert.Equal("xyzwv", result1);
            var result2 = dict.GetOrAdd("a", "x", "y", "z", "w", "v", (arg1, arg2, arg3, arg4, arg5) => arg1 + arg2 + arg3 + arg4 + arg5);
            Assert.Equal("xyzwv", result2);
        }

        [Fact]
        public void ToArray_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            _ = dict.TryAdd("b", 2);
            var array = dict.ToArray();
            Assert.Equal(2, array.Length);
            var sorted = array.OrderBy(x => x.Key).ToArray();
            Assert.Equal("a", sorted[0].Key);
            Assert.Equal(1, sorted[0].Value);
            Assert.Equal("b", sorted[1].Key);
            Assert.Equal(2, sorted[1].Value);
        }

        [Fact]
        public void TryAdd_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            Assert.True(dict.TryAdd("a", 1));
            Assert.Equal(1, dict["a"]);
            Assert.False(dict.TryAdd("a", 2));
            Assert.Equal(1, dict["a"]);
        }

        [Fact]
        public void TryGetValue_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryGetValue("a", out var value));
            Assert.Equal(1, value);
            Assert.False(dict.TryGetValue("b", out _));
        }

        [Fact]
        public void TryUpdate_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryUpdate("a", 2, 1));
            Assert.Equal(2, dict["a"]);
            Assert.False(dict.TryUpdate("a", 3, 1));
            Assert.Equal(2, dict["a"]);
        }

        [Fact]
        public void TryRemove_Method()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            _ = dict.TryAdd("a", 1);
            Assert.True(dict.TryRemove("a", out var value));
            Assert.Equal(1, value);
            Assert.False(dict.ContainsKey("a"));
            Assert.False(dict.TryRemove("b", out _));
        }

        [Fact]
        public async Task GetOrAdd_ThreadSafety()
        {
            var dict = new ConcurrentFactoryDictionary<string, int>();
            var factoryCallCount = 0;
            var tasks = new Task[10];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    _ = dict.GetOrAdd("key", () =>
                    {
                        _ = System.Threading.Interlocked.Increment(ref factoryCallCount);
                        System.Threading.Thread.Sleep(10);
                        return 1;
                    });
                }, TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(tasks);
            Assert.Equal(1, factoryCallCount);
            Assert.Equal(1, dict["key"]);
        }

        [Fact]
        public void GenericInterfaces()
        {
            var dict = new ConcurrentFactoryDictionary<string, int?>();
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
            var dict = new ConcurrentFactoryDictionary<string, int?>();
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
        public void GetOrAdd_FactoryAddingTheSameKey_Throws()
        {
            //a factory that adds its own key leaves the outer add nothing to do, which means the factory recursed into the dictionary
            var dict = new ConcurrentFactoryDictionary<string, int>();
            int Add(string key) { _ = dict.TryAdd(key, 0); return 1; }

            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("a", () => Add("a")));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("b", (key) => Add(key)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("c", 1, (key, a) => Add(key)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("d", 1, 2, (key, a, b) => Add(key)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("e", 1, 2, 3, (key, a, b, c) => Add(key)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("f", 1, 2, 3, 4, (key, a, b, c, d) => Add(key)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("g", 1, 2, 3, 4, 5, (key, a, b, c, d, e) => Add(key)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("h", "h", (a) => Add(a)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("i", "i", 2, (a, b) => Add(a)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("j", "j", 2, 3, (a, b, c) => Add(a)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("k", "k", 2, 3, 4, (a, b, c, d) => Add(a)));
            _ = Assert.Throws<InvalidOperationException>(() => dict.GetOrAdd("l", "l", 2, 3, 4, 5, (a, b, c, d, e) => Add(a)));

            var array = new KeyValuePair<string, int>[dict.Count + 1];
            ((ICollection<KeyValuePair<string, int>>)dict).CopyTo(array, 1);
            Assert.Equal(dict.Count, array.Count(x => x.Key is not null));
        }
    }
}
