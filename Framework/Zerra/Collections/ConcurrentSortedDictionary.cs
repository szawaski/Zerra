// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Linq;

namespace Zerra.Collections
{
    public class ConcurrentSortedDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary
         where TKey : notnull
    {
        private readonly object locker = new();
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
            lock (locker)
            {
                ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                lock (locker)
                {
                    var keys = dictionary.Keys.ToArray();
                    return keys;
                }
            }
        }
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                lock (locker)
                {
                    var values = dictionary.Values.ToArray();
                    return values;
                }
            }
        }
        int ICollection.Count
        {
            get
            {
                lock (locker)
                {
                    var count = dictionary.Count;
                    return count;
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
                lock (locker)
                {
                    var keys = dictionary.Keys.ToArray();
                    return keys;
                }
            }
        }
        ICollection IDictionary.Values
        {
            get
            {
                lock (locker)
                {
                    var values = dictionary.Values.ToArray();
                    return values;
                }
            }
        }
        void ICollection.CopyTo(Array array, int index)
        {
            lock (locker)
            {
                ((ICollection)dictionary).CopyTo(array, index);
            }
        }
        object? IDictionary.this[object key]
        {
            get
            {
                if (key is not TKey keycasted)
                    throw new ArgumentException("Key is not the correct type");

                lock (locker)
                {
                    if (!dictionary.TryGetValue(keycasted, out var value))
                        throw new KeyNotFoundException();
                    return value;
                }
            }
            set
            {
                if (key is not TKey keycasted)
                    throw new ArgumentException("Key is not the correct type");
                if (value is not TValue valuecasted)
                    throw new ArgumentException("Value is not the correct type");

                lock (locker)
                {
                    dictionary[keycasted] = valuecasted;
                }
            }
        }
        void IDictionary.Add(object key, object? value)
        {
            if (key is not TKey keycasted)
                throw new ArgumentException("Key is not the correct type");
            if (value is not TValue valuecasted)
                throw new ArgumentException("Value is not the correct type");

            lock (locker)
            {
                if (dictionary.ContainsKey(keycasted))
                {
                    throw new ArgumentException("An element with the same key already exists");
                }
                dictionary.Add(keycasted, valuecasted);
            }
        }
        void IDictionary.Clear()
        {
            lock (locker)
            {
                dictionary.Clear();
            }
        }
        bool IDictionary.Contains(object key)
        {
            if (key is not TKey casted)
                return false;
            lock (locker)
            {
                var contains = dictionary.ContainsKey(casted);
                return contains;
            }
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            lock (locker)
            {
                var enumerator = new ConcurrentSortedDictionaryEnumerator(dictionary.ToArray().AsEnumerable().GetEnumerator());
                return enumerator;
            }
        }
        void IDictionary.Remove(object key)
        {
            if (key is not TKey casted)
                throw new KeyNotFoundException();
            lock (locker)
            {
                if (!dictionary.ContainsKey(casted))
                {
                    throw new KeyNotFoundException();
                }
                _ = dictionary.Remove(casted);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (locker)
                {
                    if (!dictionary.TryGetValue(key, out var value))
                        throw new KeyNotFoundException();
                    return value;
                }
            }
            set
            {
                lock (locker)
                {
                    dictionary[key] = value;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (locker)
                {
                    var isempty = dictionary.Count == 0;
                    return isempty;
                }
            }
        }
        public ICollection<TKey> Keys
        {
            get
            {
                lock (locker)
                {
                    var keys = dictionary.Keys.ToArray();
                    return keys;
                }
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                lock (locker)
                {
                    var values = dictionary.Values.ToArray();
                    return values;
                }
            }
        }
        public int Count
        {
            get
            {
                lock (locker)
                {
                    var count = dictionary.Count;
                    return count;
                }
            }
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (locker)
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
        }
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (locker)
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
        }
        public void Clear()
        {
            lock (locker)
            {
                dictionary.Clear();
            }
        }
        public bool ContainsKey(TKey key)
        {
            if (key is not TKey casted)
                return false;
            lock (locker)
            {
                var contains = dictionary.ContainsKey(casted);
                return contains;
            }
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (locker)
            {
                var items = dictionary.ToArray();
                return items.AsEnumerable().GetEnumerator();
            }
        }
        public TValue GetOrAdd(TKey key, TValue value)
        {
            lock (locker)
            {
                if (!dictionary.TryGetValue(key, out var currentvalue))
                    throw new KeyNotFoundException();
                return currentvalue;
            }
        }
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            lock (locker)
            {
                if (dictionary.TryGetValue(key, out var currentvalue))
                    return currentvalue;

                var value = valueFactory(key);
                dictionary.Add(key, value);
                return value;
            }
        }
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            lock (locker)
            {
                var items = dictionary.ToArray();
                return items;
            }
        }
        public bool TryAdd(TKey key, TValue value)
        {
            lock (locker)
            {
                if (dictionary.ContainsKey(key))
                {
                    return false;
                }
                dictionary.Add(key, value);
                return true;
            }
        }
        public bool TryGetValue(TKey key,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
        out TValue value)
        {
            lock (locker)
            {
                var trygetvalue = dictionary.TryGetValue(key, out value);
                return trygetvalue;
            }
        }
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue)
        {
            lock (locker)
            {
                if (!dictionary.TryGetValue(key, out var currentvalue))
                    return false;
                
                if (currentvalue != null && comparisonValue != null && !currentvalue.Equals(comparisonValue))
                    return false;
                dictionary[key] = value;
                return true;
            }
        }
        public bool TryRemove(TKey key,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out TValue value)
        {
            lock (locker)
            {
                if (!dictionary.TryGetValue(key, out value))
                    return false;

                if (!dictionary.Remove(key))
                    return false;
                return true;
            }
        }

        private sealed class ConcurrentSortedDictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;
            public ConcurrentSortedDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
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