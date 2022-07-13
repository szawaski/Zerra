// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Zerra.Collections
{
    public class ConcurrentSortedDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary, IDisposable
    {
        private readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly SortedDictionary<TKey, TValue> dictionary;

        public ConcurrentSortedDictionary()
        {
            this.dictionary = new SortedDictionary<TKey, TValue>();
        }
        public ConcurrentSortedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(dictionary);
        }
        public ConcurrentSortedDictionary(IComparer<TKey> comparer)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(comparer);
        }
        public ConcurrentSortedDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(dictionary, comparer);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) { _ = TryAdd(key, value); }
        bool IDictionary<TKey, TValue>.Remove(TKey key) { return TryRemove(key, out _); }
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) { _ = TryAdd(item.Key, item.Value); }
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) { return TryRemove(item.Key, out _); }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) { return ContainsKey(item.Key); }
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).IsReadOnly;
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            locker.EnterReadLock();
            ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
            locker.ExitReadLock();
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                locker.EnterReadLock();
                var keys = dictionary.Keys.ToArray();
                locker.ExitReadLock();
                return keys;
            }
        }
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                locker.EnterReadLock();
                var values = dictionary.Values.ToArray();
                locker.ExitReadLock();
                return values;
            }
        }
        int ICollection.Count
        {
            get
            {
                locker.EnterReadLock();
                var count = dictionary.Count;
                locker.ExitReadLock();
                return count;
            }
        }
        bool ICollection.IsSynchronized => ((ICollection)dictionary).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)dictionary).SyncRoot;
        bool IDictionary.IsFixedSize => ((IDictionary)dictionary).IsFixedSize;
        bool IDictionary.IsReadOnly => ((IDictionary)dictionary).IsReadOnly;
        ICollection IDictionary.Keys
        {
            get
            {
                locker.EnterReadLock();
                var keys = dictionary.Keys.ToArray();
                locker.ExitReadLock();
                return keys;
            }
        }
        ICollection IDictionary.Values
        {
            get
            {
                locker.EnterReadLock();
                var values = dictionary.Values.ToArray();
                locker.ExitReadLock();
                return values;
            }
        }
        void ICollection.CopyTo(Array array, int index)
        {
            locker.EnterReadLock();
            ((ICollection)dictionary).CopyTo(array, index);
            locker.ExitReadLock();
        }
        object IDictionary.this[object key]
        {
            get
            {
                if (!(key is TKey keycasted))
                    throw new ArgumentException("Key is not the correct type");

                locker.EnterReadLock();
                if (!dictionary.ContainsKey(keycasted))
                {
                    locker.ExitReadLock();
                    throw new KeyNotFoundException();
                }
                var value = dictionary[keycasted];
                locker.ExitReadLock();
                return value;
            }
            set
            {
                if (!(key is TKey keycasted))
                    throw new ArgumentException("Key is not the correct type");
                if (!(value is TValue valuecasted))
                    throw new ArgumentException("Value is not the correct type");

                locker.EnterWriteLock();
                dictionary[keycasted] = valuecasted;
                locker.ExitWriteLock();
            }
        }
        void IDictionary.Add(object key, object value)
        {
            if (!(key is TKey keycasted))
                throw new ArgumentException("Key is not the correct type");
            if (!(value is TValue valuecasted))
                throw new ArgumentException("Value is not the correct type");

            locker.EnterWriteLock();
            if (dictionary.ContainsKey(keycasted))
            {
                locker.ExitWriteLock();
                throw new ArgumentException("An element with the same key already exists");
            }
            dictionary.Add(keycasted, valuecasted);
            locker.ExitWriteLock();
        }
        void IDictionary.Clear()
        {
            locker.EnterWriteLock();
            dictionary.Clear();
            locker.ExitWriteLock();
        }
        bool IDictionary.Contains(object key)
        {
            if (!(key is TKey casted))
                return false;
            locker.EnterReadLock();
            var contains = dictionary.ContainsKey(casted);
            locker.ExitReadLock();
            return contains;
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            locker.EnterReadLock();
            var enumerator = new ConcurrentSortedDictionaryEnumerator(dictionary.ToArray().AsEnumerable().GetEnumerator());
            locker.ExitReadLock();
            return enumerator;
        }
        void IDictionary.Remove(object key)
        {
            if (!(key is TKey casted))
                throw new KeyNotFoundException();
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(casted))
            {
                locker.ExitWriteLock();
                throw new KeyNotFoundException();
            }
            _ = dictionary.Remove(casted);
            locker.ExitWriteLock();
        }

        public TValue this[TKey key]
        {
            get
            {
                locker.EnterReadLock();
                if (!dictionary.ContainsKey(key))
                {
                    locker.ExitReadLock();
                    throw new KeyNotFoundException();
                }
                var value = dictionary[key];
                locker.ExitReadLock();
                return value;
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
                var isempty = dictionary.Count == 0;
                locker.ExitReadLock();
                return isempty;
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

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(key))
            {
                var addValue = addValueFactory(key);
                dictionary.Add(key, addValue);
                locker.ExitWriteLock();
                return addValue;
            }
            var updatevalue = updateValueFactory(key, addValueFactory(key));
            dictionary[key] = updatevalue;
            locker.ExitWriteLock();
            return updatevalue;
        }
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, addValue);
                locker.ExitWriteLock();
                return addValue;
            }
            var updatevalue = updateValueFactory(key, addValue);
            dictionary[key] = updatevalue;
            locker.ExitWriteLock();
            return updatevalue;
        }
        public void Clear()
        {
            locker.EnterWriteLock();
            dictionary.Clear();
            locker.ExitWriteLock();
        }
        public bool ContainsKey(TKey key)
        {
            if (!(key is TKey casted))
                return false;
            locker.EnterReadLock();
            var contains = dictionary.ContainsKey(casted);
            locker.ExitReadLock();
            return contains;
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            locker.EnterWriteLock();
            var items = dictionary.ToArray();
            locker.ExitWriteLock();
            return items.AsEnumerable().GetEnumerator();
        }
        public TValue GetOrAdd(TKey key, TValue value)
        {
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                locker.ExitWriteLock();
                return value;
            }
            var currentvalue = dictionary[key];
            locker.ExitWriteLock();
            return currentvalue;
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(key))
            {
                var value = valueFactory(key);
                dictionary.Add(key, value);
                locker.ExitWriteLock();
                return value;
            }
            var currentvalue = dictionary[key];
            locker.ExitWriteLock();
            return currentvalue;
        }
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            locker.EnterWriteLock();
            var items = dictionary.ToArray();
            locker.ExitWriteLock();
            return items;
        }
        public bool TryAdd(TKey key, TValue value)
        {
            locker.EnterWriteLock();
            if (dictionary.ContainsKey(key))
            {
                locker.ExitWriteLock();
                return false;
            }
            dictionary.Add(key, value);
            locker.ExitWriteLock();
            return true;
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            locker.EnterReadLock();
            var trygetvalue = dictionary.TryGetValue(key, out value);
            locker.ExitReadLock();
            return trygetvalue;
        }
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue)
        {
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(key))
            {
                locker.ExitWriteLock();
                return false;
            }
            var currentvalue = dictionary[key];
            if (!currentvalue.Equals(comparisonValue))
                return false;
            dictionary[key] = value;
            locker.ExitWriteLock();
            return true;
        }
        public bool TryRemove(TKey key, out TValue value)
        {
            locker.EnterWriteLock();
            if (!dictionary.ContainsKey(key))
            {
                locker.ExitWriteLock();
                value = default;
                return false;
            }
            value = dictionary[key];
            if (!dictionary.Remove(key))
            {
                locker.ExitWriteLock();
                value = default;
                return false;
            }
            locker.ExitWriteLock();
            return true;
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        ~ConcurrentSortedDictionary()
        {
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            locker.Dispose();
        }

        private class ConcurrentSortedDictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
            public ConcurrentSortedDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                this.enumerator = enumerator;
            }

            public object Current { get; private set; }
            public object Key { get; private set; }
            public object Value { get; private set; }
            public DictionaryEntry Entry => new DictionaryEntry(Key, Value);

            public bool MoveNext()
            {
                var movenext = enumerator.MoveNext();
                Current = enumerator.Current;

                Key = enumerator.Current.Key;
                Value = enumerator.Current.Value;
                return movenext;
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }
    }
}