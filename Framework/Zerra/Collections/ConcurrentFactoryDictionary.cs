// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace Zerra.Collections
{
    /// <summary>
    /// A thread-safe dictionary that will only run a factory once and other threads must wait. System.Collections.Concurrent.ConcurrentDictionary may run the same keyed factories simultaneously.
    /// </summary>
    public class ConcurrentFactoryDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary
         where TKey : notnull
    {
        private static int DefaultConcurrencyLevel => Environment.ProcessorCount;
        private const int DefaultCapacity = 31;

        private readonly ConcurrentDictionary<TKey, TValue> dictionary;
        private readonly object[] factoryLocks; //we aren't going to worry about resizing
        private readonly IEqualityComparer<TKey>? comparer;

        private uint GetFactoryLockNumber(TKey key)
        {
            var hashcode = comparer is not null ? comparer.GetHashCode(key) : key.GetHashCode();
            return (uint)hashcode % (uint)factoryLocks.Length;
        }

        public ConcurrentFactoryDictionary()
            : this(DefaultConcurrencyLevel, DefaultCapacity, null, null) { }
        public ConcurrentFactoryDictionary(int concurrencyLevel, int capacity)
            : this(concurrencyLevel, capacity, null, null) { }
        public ConcurrentFactoryDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(DefaultConcurrencyLevel, DefaultCapacity, collection, null) { }
        public ConcurrentFactoryDictionary(IEqualityComparer<TKey> comparer)
            : this(DefaultConcurrencyLevel, DefaultCapacity, null, comparer) { }
        public ConcurrentFactoryDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(DefaultConcurrencyLevel, DefaultCapacity, collection, comparer) { }
        public ConcurrentFactoryDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, DefaultCapacity, collection, comparer) { }
        public ConcurrentFactoryDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, capacity, null, comparer) { }

#if NET8_0_OR_GREATER
        private static readonly FieldInfo _tablesField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo _locksField = _tablesField.FieldType.GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo _comparerField = _tablesField.FieldType.GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance)!;
#elif NET6_0 || NET7_0
        private static readonly FieldInfo _tablesField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo _locksField = _tablesField.FieldType.GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        private static readonly FieldInfo _comparerField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance)!;
#endif

        internal ConcurrentFactoryDictionary(int concurrencyLevel, int capacity, IEnumerable<KeyValuePair<TKey, TValue>>? collection, IEqualityComparer<TKey>? comparer)
        {
            if (collection is not null)
                this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, collection, comparer);
            else
                this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity, comparer);

            this.factoryLocks = new object[concurrencyLevel];
            this.factoryLocks[0] = this.factoryLocks;
            for (var i = 1; i < this.factoryLocks.Length; i++)
                this.factoryLocks[i] = new object();

#if NET8_0_OR_GREATER
            var _tables = _tablesField.GetValue(this.dictionary);
            //this.factoryLocks = (object[])_locksField.GetValue(_tables)!; This causes threading issues
            this.comparer = (IEqualityComparer<TKey>?)_comparerField.GetValue(_tables)!;
#elif NET6_0 || NET7_0
            var _tables = _tablesField.GetValue(this.dictionary);
            //this.factoryLocks = (object[])_locksField.GetValue(_tables)!; This causes threading issues
            this.comparer = (IEqualityComparer<TKey>?)_comparerField.GetValue(this.dictionary)!;
#else
            if (comparer is not null)
            {
                if (!typeof(TKey).IsValueType || !ReferenceEquals(comparer, EqualityComparer<TKey>.Default))
                    this.comparer = comparer;
            }
            else if (!typeof(TKey).IsValueType)
            {
                this.comparer = EqualityComparer<TKey>.Default;
            }
#endif
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => TryAdd(key, value);
        bool IDictionary<TKey, TValue>.Remove(TKey key) => TryRemove(key, out _);
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => TryAdd(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => TryRemove(item.Key, out _);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly;
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        int ICollection.Count => dictionary.Count;
        bool ICollection.IsSynchronized => ((ICollection)dictionary).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;
        bool IDictionary.IsFixedSize => ((IDictionary)dictionary).IsFixedSize;
        bool IDictionary.IsReadOnly => ((IDictionary)dictionary).IsReadOnly;
        ICollection IDictionary.Keys => ((IDictionary)dictionary).Keys;
        ICollection IDictionary.Values => ((IDictionary)dictionary).Values;
        void ICollection.CopyTo(Array array, int index) => ((ICollection)dictionary).CopyTo(array, index);
        object? IDictionary.this[object key] { get => ((IDictionary)dictionary)[key]; set => ((IDictionary)dictionary)[key] = value; }
        void IDictionary.Add(object key, object? value) => ((IDictionary)dictionary).Add(key, value);
        void IDictionary.Clear() { Clear(); }
        bool IDictionary.Contains(object key) => ((IDictionary)dictionary).Contains(key);
        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)dictionary).GetEnumerator();
        void IDictionary.Remove(object key) => ((IDictionary)dictionary).Remove(key);

        public TValue this[TKey key] { get => dictionary[key]; set => dictionary[key] = value; }

        public bool IsEmpty => dictionary.IsEmpty;
        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;

        public void Clear()
        {
            for (var i = 0; i < factoryLocks.Length; i++)
                Monitor.Enter(factoryLocks[i]);

            dictionary.Clear();

            for (var i = 0; i < factoryLocks.Length; i++)
                Monitor.Exit(factoryLocks[i]);
        }
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

        public TValue GetOrAdd(TKey key, TValue value) => dictionary.GetOrAdd(key, value);
        public TValue GetOrAdd(TKey key, Func<TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory();
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }

        public TValue GetOrAdd<TArg1>(TKey key, TArg1 arg1, Func<TKey, TArg1, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key, arg1);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2>(TKey key, TArg1 arg1, TArg2 arg2, Func<TKey, TArg1, TArg2, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key, arg1, arg2);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2, TArg3>(TKey key, TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TKey, TArg1, TArg2, TArg3, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key, arg1, arg2, arg3);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2, TArg3, TArg4>(TKey key, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TKey, TArg1, TArg2, TArg3, TArg4, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key, arg1, arg2, arg3, arg4);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2, TArg3, TArg4, TArg5>(TKey key, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TKey, TArg1, TArg2, TArg3, TArg4, TArg5, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(key, arg1, arg2, arg3, arg4, arg5);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }

        public TValue GetOrAdd<TArg1>(TKey key, TArg1 arg1, Func<TArg1, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(arg1);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2>(TKey key, TArg1 arg1, TArg2 arg2, Func<TArg1, TArg2, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(arg1, arg2);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2, TArg3>(TKey key, TArg1 arg1, TArg2 arg2, TArg3 arg3, Func<TArg1, TArg2, TArg3, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(arg1, arg2, arg3);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2, TArg3, TArg4>(TKey key, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<TArg1, TArg2, TArg3, TArg4, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(arg1, arg2, arg3, arg4);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }
        public TValue GetOrAdd<TArg1, TArg2, TArg3, TArg4, TArg5>(TKey key, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TValue> valueFactory)
        {
            //checking before locking for faster gets than adds
            if (dictionary.TryGetValue(key, out var value))
                return value;

            var lockNum = GetFactoryLockNumber(key);
            lock (factoryLocks[lockNum])
            {
                if (dictionary.TryGetValue(key, out value))
                    return value;

                value = valueFactory(arg1, arg2, arg3, arg4, arg5);
                if (!dictionary.TryAdd(key, value))
                    throw new InvalidOperationException($"{nameof(ConcurrentFactoryDictionary<object, object>)} had a factory perform a recursive operation.");
                return value;
            }
        }

        public KeyValuePair<TKey, TValue>[] ToArray() => dictionary.ToArray();
        public bool TryAdd(TKey key, TValue value) => dictionary.TryAdd(key, value);
        public bool TryGetValue(TKey key,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
        out TValue value) => dictionary.TryGetValue(key, out value);
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue) => dictionary.TryUpdate(key, value, comparisonValue);
        public bool TryRemove(TKey key,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out TValue value) => dictionary.TryRemove(key, out value);
    }
}