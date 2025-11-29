// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;

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
        object? IDictionary.this[object key]
        {
            get
            {
                if (key is not TKey keycasted)
                    throw new ArgumentException("Key is not the correct type");

                locker.EnterReadLock();
                if (!dictionary.TryGetValue(keycasted, out var value))
                {
                    locker.ExitReadLock();
                    throw new KeyNotFoundException();
                }
                
                locker.ExitReadLock();
                return value;
            }
            set
            {
                if (key is not TKey keycasted)
                    throw new ArgumentException("Key is not the correct type");
                if (value is not TValue valuecasted)
                    throw new ArgumentException("Value is not the correct type");

                locker.EnterWriteLock();
                dictionary[keycasted] = valuecasted;
                locker.ExitWriteLock();
            }
        }
        void IDictionary.Add(object key, object? value)
        {
            if (key is not TKey keycasted)
                throw new ArgumentException("Key is not the correct type");
            if (value is not TValue valuecasted)
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
            if (key is not TKey casted)
                return false;
            locker.EnterReadLock();
            var contains = dictionary.ContainsKey(casted);
            locker.ExitReadLock();
            return contains;
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            locker.EnterReadLock();
            var enumerator = new ConcurrentSortedReadWriteDictionaryEnumerator(dictionary.ToArray().AsEnumerable().GetEnumerator());
            locker.ExitReadLock();
            return enumerator;
        }
        void IDictionary.Remove(object key)
        {
            if (key is not TKey casted)
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
                if (!dictionary.TryGetValue(key, out var value))
                {
                    locker.ExitReadLock();
                    throw new KeyNotFoundException();
                }
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
            if (!dictionary.TryGetValue(key, out var existing))
            {
                var addValue = addValueFactory(key);
                dictionary.Add(key, addValue);
                locker.ExitWriteLock();
                return addValue;
            }
            var updatevalue = updateValueFactory(key, existing);
            dictionary[key] = updatevalue;
            locker.ExitWriteLock();
            return updatevalue;
        }
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            if (!dictionary.TryGetValue(key, out var existing))
            {
                dictionary.Add(key, addValue);
                locker.ExitWriteLock();
                return addValue;
            }
            var updatevalue = updateValueFactory(key, existing);
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
            if (key is not TKey casted)
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
            locker.EnterReadLock();
            if (!dictionary.TryGetValue(key, out var currentvalue))
            {
                dictionary.Add(key, value);
                locker.ExitWriteLock();
                return value;
            }
            locker.ExitReadLock();
            return currentvalue;
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            locker.EnterWriteLock();
            if (!dictionary.TryGetValue(key, out var currentvalue))
            {
                var value = valueFactory(key);
                dictionary.Add(key, value);
                locker.ExitWriteLock();
                return value;
            }
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
        public bool TryGetValue(TKey key,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
        out TValue value)
        {
            locker.EnterReadLock();
            var trygetvalue = dictionary.TryGetValue(key, out value);
            locker.ExitReadLock();
            return trygetvalue;
        }
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue)
        {
            locker.EnterWriteLock();
            if (!dictionary.TryGetValue(key, out var currentvalue))
            {
                locker.ExitWriteLock();
                return false;
            }
            if (currentvalue is not null && comparisonValue is not null && !currentvalue.Equals(comparisonValue))
            {
                locker.ExitWriteLock();
                return false;
            }
            dictionary[key] = value;
            locker.ExitWriteLock();
            return true;
        }
        public bool TryRemove(TKey key,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out TValue value)
        {
            locker.EnterWriteLock();
            if (!dictionary.TryGetValue(key, out value))
            {
                locker.ExitWriteLock();
                return false;
            }
            
            if (!dictionary.Remove(key))
            {
                locker.ExitWriteLock();
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