// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe sorted generic dictionary implementation with read-write locking for optimized concurrent access.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. Must not be null.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    public class ConcurrentSortedReadWriteDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary, IDisposable
        where TKey : notnull
    {
        private readonly ReaderWriterLockSlim locker = new(LockRecursionPolicy.NoRecursion);
        private readonly SortedDictionary<TKey, TValue> dictionary;

        /// <summary>
        /// Initializes a new instance of the ConcurrentSortedReadWriteDictionary class that is empty.
        /// </summary>
        public ConcurrentSortedReadWriteDictionary()
        {
            this.dictionary = new SortedDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentSortedReadWriteDictionary class that contains elements copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are copied to the new instance.</param>
        public ConcurrentSortedReadWriteDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentSortedReadWriteDictionary class that is empty and uses the specified comparer.
        /// </summary>
        /// <param name="comparer">The IComparer&lt;T&gt; implementation to use when comparing keys.</param>
        public ConcurrentSortedReadWriteDictionary(IComparer<TKey> comparer)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the ConcurrentSortedReadWriteDictionary class that contains elements copied from the specified dictionary and uses the specified comparer.
        /// </summary>
        /// <param name="dictionary">The dictionary whose elements are copied to the new instance.</param>
        /// <param name="comparer">The IComparer&lt;T&gt; implementation to use when comparing keys.</param>
        public ConcurrentSortedReadWriteDictionary(IDictionary<TKey, TValue> dictionary, IComparer<TKey> comparer)
        {
            this.dictionary = new SortedDictionary<TKey, TValue>(dictionary, comparer);
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            locker.EnterWriteLock();
            try
            {
                dictionary.Add(key, value);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            locker.EnterWriteLock();
            try
            {
                return dictionary.Remove(key);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            locker.EnterWriteLock();
            try
            {
                dictionary.Add(item.Key, item.Value);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            locker.EnterWriteLock();
            try
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            locker.EnterReadLock();
            try
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            locker.EnterReadLock();
            try
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
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
                    return dictionary.Keys.ToArray();
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
                    return dictionary.Values.ToArray();
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }
        int ICollection.Count => Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw new NotSupportedException($"{nameof(ICollection.SyncRoot)} is not supported, the dictionary locks internally");
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    return dictionary.Keys.ToArray();
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
                    return dictionary.Values.ToArray();
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
                locker.EnterReadLock();
                try
                {
                    return ((IDictionary)dictionary)[key];
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
                    ((IDictionary)dictionary)[key] = value;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }
        void IDictionary.Add(object key, object? value)
        {
            locker.EnterWriteLock();
            try
            {
                ((IDictionary)dictionary).Add(key, value);
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
            locker.EnterReadLock();
            try
            {
                return ((IDictionary)dictionary).Contains(key);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            KeyValuePair<TKey, TValue>[] items;
            locker.EnterReadLock();
            try
            {
                items = dictionary.ToArray();
            }
            finally
            {
                locker.ExitReadLock();
            }
            return new ConcurrentSortedReadWriteDictionaryEnumerator(items.AsEnumerable().GetEnumerator());
        }
        void IDictionary.Remove(object key)
        {
            locker.EnterWriteLock();
            try
            {
                ((IDictionary)dictionary).Remove(key);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the key is not found during a get operation.</exception>
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

        /// <summary>
        /// Gets a value indicating whether the dictionary is empty.
        /// </summary>
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

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Gets the number of key-value pairs in the dictionary.
        /// </summary>
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

        /// <summary>
        /// Adds a value to the dictionary with the specified key, or updates the value if the key already exists using a factory function.
        /// </summary>
        /// <param name="key">The key to add or update.</param>
        /// <param name="addValueFactory">A function that produces a value for a new key.</param>
        /// <param name="updateValueFactory">A function that produces an updated value for an existing key.</param>
        /// <returns>The new value for the key.</returns>
        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.TryGetValue(key, out var existing))
                {
                    var addValue = addValueFactory(key);
                    dictionary.Add(key, addValue);
                    return addValue;
                }
                var updatevalue = updateValueFactory(key, existing);
                dictionary[key] = updatevalue;
                return updatevalue;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds a value to the dictionary with the specified key, or updates the value if the key already exists.
        /// </summary>
        /// <param name="key">The key to add or update.</param>
        /// <param name="addValue">The value to be added for a new key.</param>
        /// <param name="updateValueFactory">A function that produces an updated value for an existing key.</param>
        /// <returns>The new value for the key.</returns>
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            locker.EnterWriteLock();
            try
            {
                if (!dictionary.TryGetValue(key, out var existing))
                {
                    dictionary.Add(key, addValue);
                    return addValue;
                }
                var updatevalue = updateValueFactory(key, existing);
                dictionary[key] = updatevalue;
                return updatevalue;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all key-value pairs from the dictionary.
        /// </summary>
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

        /// <summary>
        /// Determines whether the dictionary contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>True if the dictionary contains the key; otherwise, false.</returns>
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

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator for the key-value pairs in the dictionary.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            locker.EnterReadLock();
            try
            {
                var items = dictionary.ToArray();
                return items.AsEnumerable().GetEnumerator();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key, or adds the value to the dictionary if the key does not exist.
        /// </summary>
        /// <param name="key">The key to get or add.</param>
        /// <param name="value">The value to be added for a new key.</param>
        /// <returns>The value associated with the key, either new or existing.</returns>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            locker.EnterReadLock();
            try
            {
                if (dictionary.TryGetValue(key, out var currentvalue))
                    return currentvalue;
            }
            finally
            {
                locker.ExitReadLock();
            }

            locker.EnterWriteLock();
            try
            {
                if (dictionary.TryGetValue(key, out var currentvalue))
                    return currentvalue;
                dictionary.Add(key, value);
                return value;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the value associated with the specified key, or adds a value to the dictionary if the key does not exist.
        /// </summary>
        /// <param name="key">The key to get or add.</param>
        /// <param name="valueFactory">A function that produces a value for a new key.</param>
        /// <returns>The value associated with the key, either new or existing.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            locker.EnterReadLock();
            try
            {
                if (dictionary.TryGetValue(key, out var currentvalue))
                    return currentvalue;
            }
            finally
            {
                locker.ExitReadLock();
            }

            locker.EnterWriteLock();
            try
            {
                if (dictionary.TryGetValue(key, out var currentvalue))
                    return currentvalue;
                var value = valueFactory(key);
                dictionary.Add(key, value);
                return value;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Creates a copy of the dictionary as an array of key-value pairs.
        /// </summary>
        /// <returns>An array of key-value pairs in the dictionary.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            locker.EnterReadLock();
            try
            {
                var items = dictionary.ToArray();
                return items;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Attempts to add a key-value pair to the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <returns>True if the key-value pair was added; false if the key already exists.</returns>
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

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The value associated with the key if found; otherwise, the default value.</param>
        /// <returns>True if the key is found; otherwise, false.</returns>
        public bool TryGetValue(TKey key,
#if !NETSTANDARD2_0
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

        /// <summary>
        /// Attempts to update the value associated with a key only if the current value matches a comparison value.
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="value">The new value for the key.</param>
        /// <param name="comparisonValue">The value that is compared to the existing value.</param>
        /// <returns>True if the update succeeded; false if the current value does not match the comparison value or the key does not exist.</returns>
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
                {
                    return false;
                }
                dictionary[key] = value;
                return true;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Attempts to remove and return the value with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to remove.</param>
        /// <param name="value">The removed value if the key is found; otherwise, the default value.</param>
        /// <returns>True if the key was found and removed; otherwise, false.</returns>
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

        /// <summary>
        /// Releases all resources used by the dictionary.
        /// </summary>
        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer to ensure proper cleanup of resources.
        /// </summary>
        ~ConcurrentSortedReadWriteDictionary()
        {
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            locker.Dispose();
        }

        /// <summary>
        /// Enumerator for ConcurrentSortedReadWriteDictionary that implements IDictionaryEnumerator.
        /// </summary>
        private sealed class ConcurrentSortedReadWriteDictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

            /// <summary>
            /// Initializes a new instance of the ConcurrentSortedReadWriteDictionaryEnumerator class.
            /// </summary>
            /// <param name="enumerator">The underlying enumerator for key-value pairs.</param>
            public ConcurrentSortedReadWriteDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
            {
                this.enumerator = enumerator;
            }

            /// <summary>
            /// Gets the key of the current dictionary entry.
            /// </summary>
            public object Key => enumerator.Current.Key;

            /// <summary>
            /// Gets the current element in the enumeration.
            /// </summary>
            public object? Current => enumerator.Current;

            /// <summary>
            /// Gets the value of the current dictionary entry.
            /// </summary>
            public object? Value => enumerator.Current.Value;

            /// <summary>
            /// Gets a DictionaryEntry containing the current key-value pair.
            /// </summary>
            public DictionaryEntry Entry => new(Key, Value);

            /// <summary>
            /// Advances the enumerator to the next element.
            /// </summary>
            /// <returns>True if the enumerator was successfully advanced; otherwise, false.</returns>
            public bool MoveNext()
            {
                var movenext = enumerator.MoveNext();
                return movenext;
            }

            /// <summary>
            /// Resets the enumerator to its initial position.
            /// </summary>
            public void Reset()
            {
                enumerator.Reset();
            }
        }
    }
}