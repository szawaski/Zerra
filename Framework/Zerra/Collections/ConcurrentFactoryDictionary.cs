// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Zerra.Collections
{
    /// <summary>
    /// A thread-safe dictionary that will only run a factory once and other threads must wait. System.Collections.Concurrent.ConcurrentDictionary may run the same keyed factories simultaneously.
    /// </summary>
    public class ConcurrentFactoryDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary
    {
        private readonly ConcurrentDictionary<TKey, object> factoryLocks = new ConcurrentDictionary<TKey, object>();
        private readonly ConcurrentDictionary<TKey, TValue> dictionary;

        private object GetFactoryLock(TKey key)
        {
            return factoryLocks.GetOrAdd(key, (k) => { return new object(); });
        }
        private void RemoveFactoryLock(TKey key)
        {
            factoryLocks.TryRemove(key, out _);
        }
        private void ClearFactoryLocks()
        {
            factoryLocks.Clear();
        }

        public ConcurrentFactoryDictionary()
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>();
        }
        public ConcurrentFactoryDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(collection);
        }
        public ConcurrentFactoryDictionary(IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
        }
        public ConcurrentFactoryDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(collection, comparer);
        }
        public ConcurrentFactoryDictionary(int concurrencyLevel, int capacity)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity);
        }
        public ConcurrentFactoryDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, collection, comparer);
        }
        public ConcurrentFactoryDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity, comparer);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) { TryAdd(key, value); }
        bool IDictionary<TKey, TValue>.Remove(TKey key) { return TryRemove(key, out _); }
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { TryAdd(item.Key, item.Value); }
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) { return TryRemove(item.Key, out _); }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) { return ContainsKey(item.Key); }
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly;
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
        int ICollection.Count => dictionary.Count;
        bool ICollection.IsSynchronized => ((ICollection)dictionary).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;
        bool IDictionary.IsFixedSize => ((IDictionary)dictionary).IsFixedSize;
        bool IDictionary.IsReadOnly => ((IDictionary)dictionary).IsReadOnly;
        ICollection IDictionary.Keys => ((IDictionary)dictionary).Keys;
        ICollection IDictionary.Values => ((IDictionary)dictionary).Values;
        void ICollection.CopyTo(Array array, int index) { ((ICollection)dictionary).CopyTo(array, index); }
        object IDictionary.this[object key] { get => ((IDictionary)dictionary)[key]; set => ((IDictionary)dictionary)[key] = value; }
        void IDictionary.Add(object key, object value) { ((IDictionary)dictionary).Add(key, value); }
        void IDictionary.Clear() { Clear(); }
        bool IDictionary.Contains(object key) { return ((IDictionary)dictionary).Contains(key); }
        IDictionaryEnumerator IDictionary.GetEnumerator() { return ((IDictionary)dictionary).GetEnumerator(); }
        void IDictionary.Remove(object key) { ((IDictionary)dictionary).Remove(key); }

        public TValue this[TKey key] { get => dictionary[key]; set => dictionary[key] = value; }

        public bool IsEmpty => dictionary.IsEmpty;
        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            TValue wrapperAddValueFactory(TKey factoryKey)
            {
                lock (GetFactoryLock(factoryKey))
                {
                    if (dictionary.TryGetValue(factoryKey, out TValue value))
                        return value;

                    value = addValueFactory(factoryKey);
                    dictionary.TryAdd(factoryKey, value);
                    return value;
                }
            }
            TValue wrapperUpdateValueFactory(TKey factoryKey, TValue value)
            {
                lock (GetFactoryLock(factoryKey))
                {
                    var newValue = updateValueFactory(factoryKey, value);
                    dictionary.TryUpdate(factoryKey, newValue, value);
                    return value;
                }
            }
            return dictionary.AddOrUpdate(key, wrapperAddValueFactory, wrapperUpdateValueFactory);
        }
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            TValue wrapperUpdateValueFactory(TKey factoryKey, TValue value)
            {
                lock (GetFactoryLock(factoryKey))
                {
                    var newValue = updateValueFactory(factoryKey, value);
                    dictionary.TryUpdate(factoryKey, newValue, value);
                    return value;
                }
            }
            return dictionary.AddOrUpdate(key, addValue, wrapperUpdateValueFactory);
        }
        public void Clear()
        {
            dictionary.Clear();
            ClearFactoryLocks();
        }
        public bool ContainsKey(TKey key) { return dictionary.ContainsKey(key); }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return dictionary.GetEnumerator(); }
        public TValue GetOrAdd(TKey key, TValue value) { return dictionary.GetOrAdd(key, value); }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            TValue wrapperValueFactory(TKey factoryKey)
            {
                lock (GetFactoryLock(factoryKey))
                {
                    if (dictionary.TryGetValue(factoryKey, out TValue value))
                        return value;

                    value = valueFactory(factoryKey);
                    dictionary.TryAdd(factoryKey, value);
                    return value;
                }
            }
            return dictionary.GetOrAdd(key, wrapperValueFactory);
        }
        public KeyValuePair<TKey, TValue>[] ToArray() { return dictionary.ToArray(); }
        public bool TryAdd(TKey key, TValue value) { return dictionary.TryAdd(key, value); }
        public bool TryGetValue(TKey key, out TValue value) { return dictionary.TryGetValue(key, out value); }
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue) { return dictionary.TryUpdate(key, value, comparisonValue); }
        public bool TryRemove(TKey key, out TValue value)
        {
            bool removed = dictionary.TryRemove(key, out value);
            RemoveFactoryLock(key);
            return removed;
        }
    }
}