// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zerra.Collections;

namespace Zerra.TestDev
{
    public static class DictionaryThreadTest
    {
        private static readonly Dictionary<int, DateTime> test = new();
        private static DateTime Get(int id)
        {
            if (!test.TryGetValue(id, out var value))
            {
                lock (test)
                {
                    if (!test.TryGetValue(id, out value))
                    {
                        value = DateTime.UtcNow;
                        test.Add(id, value);
                        System.Threading.Thread.Sleep(200);
                    }
                }
            }
            return value;
        }

        private static void DoStuff()
        {
            for (var a = 0; a < 100; a++)
            {
                var v = Get(0);
            }
        }

        public static void ShouldFail()
        {
            for (var a = 0; a < 100; a++)
            {
                _ = Task.Run(() =>
                {
                    DoStuff();
                });
            }
        }

        private static readonly ConcurrentFactoryDictionary<int, int> atomic1 = new();
        private static readonly ConcurrentAtomicDictionary2<int, int> atomic2 = new();
        private static readonly ConcurrentDictionary<int, int> regular = new();
        private const int loops = 1000;
        private const int presleep = 1;
        private const int factorysleep = 50;
        private static int exesAtomic1 = 0;
        private static int exesAtomic2 = 0;
        private static int exesRegular = 0;
        public static void ConcurrentTest()
        {
            Regular();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
            Atomic1();
            Atomic2();
        }

        private static void Regular()
        {
            var timer = Stopwatch.StartNew();
            var tasks = new List<Task>();
            for (var a = 1; a < loops; a++)
            {
                var i = (int)Math.Ceiling(a / (loops / 10m));
                var task = Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(presleep);
                    _ = regular.GetOrAdd(i, (key) =>
                    {
                        lock (regular)
                        {
                            exesRegular++;
                        }
                        System.Threading.Thread.Sleep(factorysleep);
                        return 1234;
                    });
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            timer.Stop();
            Console.WriteLine($"regular {exesRegular} {timer.ElapsedMilliseconds}ms");
            regular.Clear();
            exesRegular = 0;
        }

        private static void Atomic1()
        {
            var timer = Stopwatch.StartNew();
            var tasks = new List<Task>();
            for (var a = 1; a < loops; a++)
            {
                var i = (int)Math.Ceiling(a / (loops / 10m));
                var task = Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(presleep);
                    _ = atomic1.GetOrAdd(i, (key) =>
                    {
                        lock (atomic1)
                        {
                            exesAtomic1++;
                        }
                        System.Threading.Thread.Sleep(factorysleep);
                        return 1234;
                    });
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            timer.Stop();
            Console.WriteLine($"atomic1 {exesAtomic1} {timer.ElapsedMilliseconds}ms");
            atomic1.Clear();
            exesAtomic1 = 0;
        }

        private static void Atomic2()
        {
            var timer = Stopwatch.StartNew();
            var tasks = new List<Task>();
            for (var a = 1; a < loops; a++)
            {
                var i = (int)Math.Ceiling(a / (loops / 10m));
                var task = Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(presleep);
                    _ = atomic2.GetOrAdd(i, (key) =>
                    {
                        lock (atomic2)
                        {
                            exesAtomic2++;
                        }
                        System.Threading.Thread.Sleep(factorysleep);
                        return 1234;
                    });
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            timer.Stop();
            Console.WriteLine($"atomic2 {exesAtomic2} {timer.ElapsedMilliseconds}ms");
            atomic2.Clear();
            exesAtomic2 = 0;
        }
    }

    public class ConcurrentAtomicDictionary2<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        private readonly ReaderWriterLockSlim locker = new(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<TKey, TValue> dictionary;

        public ConcurrentAtomicDictionary2()
        {
            this.dictionary = new Dictionary<TKey, TValue>();
        }
        public ConcurrentAtomicDictionary2(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary);
        }
        public ConcurrentAtomicDictionary2(IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new Dictionary<TKey, TValue>(comparer);
        }
        public ConcurrentAtomicDictionary2(int capacity)
        {
            this.dictionary = new Dictionary<TKey, TValue>(capacity);
        }
        public ConcurrentAtomicDictionary2(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }
        public ConcurrentAtomicDictionary2(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            _ = TryAdd(key, value);
        }
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return TryRemove(key, out _);
        }
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            _ = TryAdd(item.Key, item.Value);
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return TryRemove(item.Key, out _);
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            locker.EnterReadLock();
            ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
            locker.ExitReadLock();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                locker.EnterReadLock();
                if (dictionary.TryGetValue(key, out var value))
                {
                    locker.ExitReadLock();
                    return value;
                }
                locker.ExitReadLock();

                throw new KeyNotFoundException();
            }
            set
            {
                locker.EnterWriteLock();
                dictionary[key] = value;
                locker.ExitWriteLock();
            }
        }

        public bool IsEmpty
        {
            get
            {
                locker.EnterReadLock();
                var isEmpty = dictionary.Count == 0;
                locker.ExitReadLock();
                return isEmpty;
            }
        }
        public ICollection<TKey> Keys
        {
            get
            {
                locker.EnterReadLock();
                var keys = dictionary.Keys.ToArray();
                locker.ExitReadLock();
                return keys;
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                locker.EnterReadLock();
                var values = dictionary.Values.ToArray();
                locker.ExitReadLock();
                return values;
            }
        }
        public int Count
        {
            get
            {
                locker.EnterReadLock();
                var count = dictionary.Count;
                locker.ExitReadLock();
                return count;
            }
        }

        public object this[object key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterUpgradeableReadLock();
            if (dictionary.TryGetValue(key, out var value))
            {
                var newValue = updateValueFactory(key, value);
                locker.EnterWriteLock();
                dictionary[key] = newValue;
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
                return newValue;
            }
            else
            {
                var addValue = addValueFactory(key);
                locker.EnterWriteLock();
                dictionary.Add(key, addValue);
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
                return addValue;
            }
        }
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterUpgradeableReadLock();
            if (dictionary.TryGetValue(key, out var value))
            {
                var newValue = updateValueFactory(key, value);
                locker.EnterWriteLock();
                dictionary[key] = newValue;
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
                return newValue;
            }
            else
            {
                locker.EnterWriteLock();
                dictionary.Add(key, addValue);
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
                return addValue;
            }
        }
        public void Clear()
        {
            locker.EnterWriteLock();
            dictionary.Clear();
            locker.ExitWriteLock();
        }
        public bool ContainsKey(TKey key)
        {
            locker.EnterReadLock();
            var containsKey = dictionary.ContainsKey(key);
            locker.ExitReadLock();
            return containsKey;
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)ToArray()).GetEnumerator();
        }
        public TValue GetOrAdd(TKey key, TValue value)
        {
            locker.EnterUpgradeableReadLock();
            if (dictionary.TryGetValue(key, out var currentValue))
            {
                locker.ExitUpgradeableReadLock();
                return currentValue;
            }

            locker.EnterWriteLock();

            if (dictionary.TryGetValue(key, out currentValue))
            {
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
                return currentValue;
            }

            dictionary.Add(key, value);

            locker.ExitWriteLock();
            locker.ExitUpgradeableReadLock();

            return value;
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            locker.EnterUpgradeableReadLock();
            if (dictionary.TryGetValue(key, out var value))
            {
                locker.ExitUpgradeableReadLock();
                return value;
            }

            locker.EnterWriteLock();

            if (dictionary.TryGetValue(key, out value))
            {
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
                return value;
            }

            try
            {
                value = valueFactory.Invoke(key);
                dictionary.Add(key, value);
            }
            finally
            {
                locker.ExitWriteLock();
                locker.ExitUpgradeableReadLock();
            }

            return value;
        }
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            locker.EnterReadLock();
            var values = dictionary.ToArray();
            locker.ExitReadLock();
            return values;
        }
        public bool TryAdd(TKey key, TValue value)
        {
            locker.EnterWriteLock();
            var added = false;
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                added = true;
            }
            locker.ExitWriteLock();
            return added;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            locker.EnterReadLock();
            var success = dictionary.TryGetValue(key, out value);
            locker.ExitReadLock();
            return success;
        }
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue)
        {
            var updated = false;
            locker.EnterUpgradeableReadLock();
            if (dictionary.TryGetValue(key, out var existingValue))
            {
                if (existingValue.Equals(comparisonValue))
                {
                    locker.EnterWriteLock();
                    dictionary[key] = value;
                    locker.ExitWriteLock();
                    updated = true;
                }
                locker.ExitUpgradeableReadLock();
            }
            locker.ExitUpgradeableReadLock();
            return updated;
        }
        public bool TryRemove(TKey key, out TValue value)
        {
            locker.EnterUpgradeableReadLock();

            if (!dictionary.TryGetValue(key, out value))
            {
                locker.ExitUpgradeableReadLock();
                return false;
            }

            locker.EnterWriteLock();
            if (dictionary.ContainsKey(key))
            {
                _ = dictionary.Remove(key);
            }
            locker.ExitWriteLock();
            locker.ExitUpgradeableReadLock();
            return true;
        }

        ~ConcurrentAtomicDictionary2()
        {
            locker.Dispose();
        }
    }
}
