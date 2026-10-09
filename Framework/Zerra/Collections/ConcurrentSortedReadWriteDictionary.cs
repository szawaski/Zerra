// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace Zerra.Collections
{
    public class ConcurrentSortedReadWriteDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary, IDisposable
        where TKey : notnull
    {
        private readonly ReaderWriterLockSlim locker = new(LockRecursionPolicy.NoRecursion);
        private readonly SortedDictionary<TKey, TValue> dictionary;

        public ConcurrentSortedReadWriteDictionary()
        {
            this.dictionary = new SortedDictionary<TKey, TValue>();
        }
        public ConcurrentSortedReadWriteDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(dictionary);
        }
        public ConcurrentSortedReadWriteDictionary(IComparer<TKey> comparer)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(comparer);
        }
        public ConcurrentSortedReadWriteDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
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
            try
            {
                ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var keys = dictionary.Keys.ToArray();
                    return keys;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var values = dictionary.Values.ToArray();
                    return values;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        int ICollection.Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var count = dictionary.Count;
                    return count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
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
                try
                {
                    var keys = dictionary.Keys.ToArray();
                    return keys;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        ICollection IDictionary.Values
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var values = dictionary.Values.ToArray();
                    return values;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        void ICollection.CopyTo(Array array, int index)
        {
            locker.EnterReadLock();
            try
            {
                ((ICollection)dictionary).CopyTo(array, index);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        object? IDictionary.this[object key]
        {
            get
            {
                if (key is not TKey keycasted)
                    throw new ArgumentException("Key is not the correct type");

                locker.EnterReadLock();
                try
                {
                    if (!dictionary.TryGetValue(keycasted, out var value))
                    {
                        throw new KeyNotFoundException();
                    }
                    
                    return value;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
            set
            {
                if (key is not TKey keycasted)
                    throw new ArgumentException("Key is not the correct type");
                if (value is not TValue valuecasted)
                    throw new ArgumentException("Value is not the correct type");

                locker.EnterWriteLock();
                try
                {
                    dictionary[keycasted] = valuecasted;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }
        void IDictionary.Add(object key, object? value)
        {
            if (key is not TKey keycasted)
                throw new ArgumentException("Key is not the correct type");
            if (value is not TValue valuecasted)
                throw new ArgumentException("Value is not the correct type");

            locker.EnterWriteLock();
            try
            {
                if (dictionary.ContainsKey(keycasted))
                {
                    throw new ArgumentException("An element with the same key already exists");
                }
                dictionary.Add(keycasted, valuecasted);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        void IDictionary.Clear()
        {
            locker.EnterWriteLock();
            try
            {
                dictionary.Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        bool IDictionary.Contains(object key)
        {
            if (key is not TKey casted)
                return false;
            locker.EnterReadLock();
            try
            {
                var contains = dictionary.ContainsKey(casted);
                return contains;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            locker.EnterReadLock();
            try
            {
                var enumerator = new ConcurrentSortedReadWriteDictionaryEnumerator(dictionary.ToArray().AsEnumerable().GetEnumerator());
                return enumerator;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        void IDictionary.Remove(object key)
        {
            if (key is not TKey casted)
                throw new KeyNotFoundException();
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.ContainsKey(casted))
                {
                    throw new KeyNotFoundException();
                }
                _ = dictionary.Remove(casted);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    if (!dictionary.TryGetValue(key, out var value))
                    {
                        throw new KeyNotFoundException();
                    }
                    return value;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
            set
            {
                locker.EnterWriteLock();
                try
                {
                    dictionary[key] = value;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var isempty = dictionary.Count == 0;
                    return isempty;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        public ICollection<TKey> Keys
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var keys = dictionary.Keys.ToArray();
                    return keys;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var values = dictionary.Values.ToArray();
                    return values;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        public int Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var count = dictionary.Count;
                    return count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.ContainsKey(key))
                {
                    var addValue = addValueFactory(key);
                    dictionary.Add(key, addValue);
                    return addValue;
                }
                var updatevalue = updateValueFactory(key, addValueFactory(key));
                dictionary[key] = updatevalue;
                return updatevalue;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, addValue);
                    return addValue;
                }
                var updatevalue = updateValueFactory(key, addValue);
                dictionary[key] = updatevalue;
                return updatevalue;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public void Clear()
        {
            locker.EnterWriteLock();
            try
            {
                dictionary.Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public bool ContainsKey(TKey key)
        {
            if (key is not TKey casted)
                return false;
            locker.EnterReadLock();
            try
            {
                var contains = dictionary.ContainsKey(casted);
                return contains;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            locker.EnterWriteLock();
            try
            {
                var items = dictionary.ToArray();
                return items.AsEnumerable().GetEnumerator();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public TValue GetOrAdd(TKey key, TValue value)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.TryGetValue(key, out var currentvalue))
                {
                    dictionary.Add(key, value);
                    return value;
                }
                return currentvalue;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.TryGetValue(key, out var currentvalue))
                {
                    var value = valueFactory(key);
                    dictionary.Add(key, value);
                    return value;
                }
                return currentvalue;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            locker.EnterWriteLock();
            try
            {
                var items = dictionary.ToArray();
                return items;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public bool TryAdd(TKey key, TValue value)
        {
            locker.EnterWriteLock();
            try
            {
                if (dictionary.ContainsKey(key))
                {
                    return false;
                }
                dictionary.Add(key, value);
                return true;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public bool TryGetValue(TKey key,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
        out TValue value)
        {
            locker.EnterReadLock();
            try
            {
                var trygetvalue = dictionary.TryGetValue(key, out value);
                return trygetvalue;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.TryGetValue(key, out var currentvalue))
                {
                    return false;
                }
                if (!EqualityComparer<TValue>.Default.Equals(currentvalue, comparisonValue))
                    return false;
                dictionary[key] = value;
                return true;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public bool TryRemove(TKey key,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out TValue value)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.TryGetValue(key, out value))
                {
                    return false;
                }
                
                if (!dictionary.Remove(key))
                {
                    return false;
                }
                return true;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        ~ConcurrentSortedReadWriteDictionary()
        {
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            locker.Dispose();
        }

        private sealed class ConcurrentSortedReadWriteDictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
            public ConcurrentSortedReadWriteDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                this.enumerator = enumerator;
            }

            public object Key => enumerator.Current.Key;
            public object? Current => enumerator.Current;
            public object? Value => enumerator.Current.Value;

            public DictionaryEntry Entry => new(Key, Value);

            public bool MoveNext()
            {
                var movenext = enumerator.MoveNext();
                return movenext;
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }
    }
}