// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Zerra.Collections
{
    /// <summary>
    /// A thread-safe dictionary that ensures each factory is executed only once, with other threads waiting for completion.
    /// Unlike <see cref="ConcurrentDictionary{TKey, TValue}"/>, which may run the same keyed factories simultaneously, this implementation guarantees single execution per key.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public class ConcurrentFactoryDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>, ICollection, IDictionary
         where TKey : notnull
    {
        private static int DefaultConcurrencyLevel => Environment.ProcessorCount;
        private const int DefaultCapacity = 31;

        private readonly ConcurrentDictionary<TKey, TValue> dictionary;
        private readonly object[] factoryLocks; //we aren't going to worry about resizing
        private readonly IEqualityComparer<TKey>? comparer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint GetFactoryLockNumber(TKey key)
        {
            var hashcode = comparer is not null ? comparer.GetHashCode(key) : key.GetHashCode();
            return (uint)hashcode % (uint)factoryLocks.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class.
        /// </summary>
        public ConcurrentFactoryDictionary()
            : this(DefaultConcurrencyLevel, DefaultCapacity, null, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class with the specified concurrency level and capacity.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
        /// <param name="capacity">The initial capacity of the dictionary.</param>
        public ConcurrentFactoryDictionary(int concurrencyLevel, int capacity)
            : this(concurrencyLevel, capacity, null, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class with the specified collection.
        /// </summary>
        /// <param name="collection">The collection of key-value pairs to add to the dictionary.</param>
        public ConcurrentFactoryDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(DefaultConcurrencyLevel, DefaultCapacity, collection, null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class with the specified equality comparer.
        /// </summary>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        public ConcurrentFactoryDictionary(IEqualityComparer<TKey> comparer)
            : this(DefaultConcurrencyLevel, DefaultCapacity, null, comparer) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class with the specified collection and equality comparer.
        /// </summary>
        /// <param name="collection">The collection of key-value pairs to add to the dictionary.</param>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        public ConcurrentFactoryDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(DefaultConcurrencyLevel, DefaultCapacity, collection, comparer) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class with the specified concurrency level, collection, and equality comparer.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
        /// <param name="collection">The collection of key-value pairs to add to the dictionary.</param>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        public ConcurrentFactoryDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, DefaultCapacity, collection, comparer) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentFactoryDictionary{TKey, TValue}"/> class with the specified concurrency level, capacity, and equality comparer.
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
        /// <param name="capacity">The initial capacity of the dictionary.</param>
        /// <param name="comparer">The equality comparer to use for keys.</param>
        public ConcurrentFactoryDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(concurrencyLevel, capacity, null, comparer) { }

//#if NET8_0_OR_GREATER
//        private static readonly FieldInfo _tablesField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance)!;
//        //private static readonly FieldInfo _locksField = _tablesField.FieldType.GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!;
//        private static readonly FieldInfo _comparerField = _tablesField.FieldType.GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance)!;
//#elif NET6_0 || NET7_0
//        private static readonly FieldInfo _tablesField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("_tables", BindingFlags.NonPublic | BindingFlags.Instance)!;
//        //private static readonly FieldInfo _locksField = _tablesField.FieldType.GetField("_locks", BindingFlags.NonPublic | BindingFlags.Instance)!;
//        private static readonly FieldInfo _comparerField = typeof(ConcurrentDictionary<TKey, TValue>).GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance)!;
//#endif

        internal ConcurrentFactoryDictionary(int concurrencyLevel, int capacity, IEnumerable<KeyValuePair<TKey, TValue>>? collection, IEqualityComparer<TKey>? comparer)
        {
#if NET8_0_OR_GREATER
            if (collection is not null)
                this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, collection, comparer);
            else
                this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity, comparer);
#else
            if (collection is not null)
                this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, collection, comparer ?? EqualityComparer<TKey>.Default);
            else
                this.dictionary = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity, comparer ?? EqualityComparer<TKey>.Default);
#endif 

            this.factoryLocks = new object[concurrencyLevel];
            this.factoryLocks[0] = this.factoryLocks;
            for (var i = 1; i < this.factoryLocks.Length; i++)
                this.factoryLocks[i] = new object();

            //#if NET8_0_OR_GREATER
            //            var _tables = _tablesField.GetValue(this.dictionary);
            //            //this.factoryLocks = (object[])_locksField.GetValue(_tables)!; This causes threading issues
            //            this.comparer = (IEqualityComparer<TKey>?)_comparerField.GetValue(_tables)!;
            //#elif NET6_0 || NET7_0
            //            var _tables = _tablesField.GetValue(this.dictionary);
            //            //this.factoryLocks = (object[])_locksField.GetValue(_tables)!; This causes threading issues
            //            this.comparer = (IEqualityComparer<TKey>?)_comparerField.GetValue(this.dictionary)!;
            //#else


            //if (comparer is not null)
            //{
            //    if (!typeof(TKey).IsValueType || !ReferenceEquals(comparer, EqualityComparer<TKey>.Default))
            //        this.comparer = comparer;
            //}
            //else if (!typeof(TKey).IsValueType)
            //{
            //    this.comparer = EqualityComparer<TKey>.Default;
            //}
            this.comparer = dictionary.Comparer;
            //#endif
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

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the key.</returns>
        public TValue this[TKey key] { get => dictionary[key]; set => dictionary[key] = value; }

        /// <summary>
        /// Gets a value indicating whether the dictionary is empty.
        /// </summary>
        public bool IsEmpty => dictionary.IsEmpty;
        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public ICollection<TKey> Keys => dictionary.Keys;
        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public ICollection<TValue> Values => dictionary.Values;
        /// <summary>
        /// Gets the number of key-value pairs in the dictionary.
        /// </summary>
        public int Count => dictionary.Count;

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < factoryLocks.Length; i++)
                Monitor.Enter(factoryLocks[i]);

            dictionary.Clear();

            for (var i = 0; i < factoryLocks.Length; i++)
                Monitor.Exit(factoryLocks[i]);
        }
        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>true if the dictionary contains the key; otherwise, false.</returns>
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns>An enumerator for the dictionary.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

        /// <summary>
        /// Gets or adds a value with the specified key. If the key exists, returns the existing value; otherwise, adds the new value.
        /// </summary>
        /// <param name="key">The key to get or add.</param>
        /// <param name="value">The value to add if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
        public TValue GetOrAdd(TKey key, TValue value) => dictionary.GetOrAdd(key, value);
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <param name="key">The key to get or add.</param>
        /// <param name="valueFactory">A factory function that creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <param name="key">The key to get or add.</param>
        /// <param name="valueFactory">A factory function that receives the key and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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

        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and argument and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="arg3">The third argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="arg3">The third argument to pass to the factory.</param>
        /// <param name="arg4">The fourth argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <typeparam name="TArg5">The type of the fifth factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="arg3">The third argument to pass to the factory.</param>
        /// <param name="arg4">The fourth argument to pass to the factory.</param>
        /// <param name="arg5">The fifth argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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

        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="arg3">The third argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="arg3">The third argument to pass to the factory.</param>
        /// <param name="arg4">The fourth argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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
        /// <summary>
        /// Gets or adds a value with the specified key using the provided factory. If the key exists, returns the existing value; otherwise, calls the factory to create the value.
        /// The factory is guaranteed to execute only once per key, with other threads waiting for completion.
        /// </summary>
        /// <typeparam name="TArg1">The type of the first factory argument.</typeparam>
        /// <typeparam name="TArg2">The type of the second factory argument.</typeparam>
        /// <typeparam name="TArg3">The type of the third factory argument.</typeparam>
        /// <typeparam name="TArg4">The type of the fourth factory argument.</typeparam>
        /// <typeparam name="TArg5">The type of the fifth factory argument.</typeparam>
        /// <param name="key">The key to get or add.</param>
        /// <param name="arg1">The first argument to pass to the factory.</param>
        /// <param name="arg2">The second argument to pass to the factory.</param>
        /// <param name="arg3">The third argument to pass to the factory.</param>
        /// <param name="arg4">The fourth argument to pass to the factory.</param>
        /// <param name="arg5">The fifth argument to pass to the factory.</param>
        /// <param name="valueFactory">A factory function that receives the key and arguments and creates the value if the key does not exist.</param>
        /// <returns>The value associated with the key.</returns>
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

        /// <summary>
        /// Converts the dictionary to an array of key-value pairs.
        /// </summary>
        public KeyValuePair<TKey, TValue>[] ToArray() => dictionary.ToArray();
        /// <summary>
        /// Attempts to add the specified key-value pair to the dictionary.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>true if the key-value pair was added; false if the key already exists.</returns>
        public bool TryAdd(TKey key, TValue value) => dictionary.TryAdd(key, value);
        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="value">When this method returns, contains the value associated with the key, if found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns>true if the key was found; otherwise, false.</returns>
        public bool TryGetValue(TKey key,
#if NET5_0_OR_GREATER
            [MaybeNullWhen(false)]
#endif
        out TValue value) => dictionary.TryGetValue(key, out value);
        /// <summary>
        /// Attempts to update the value for the specified key if the current value matches the comparison value.
        /// </summary>
        /// <param name="key">The key of the value to update.</param>
        /// <param name="value">The new value.</param>
        /// <param name="comparisonValue">The value to compare with the existing value.</param>
        /// <returns>true if the update succeeded; false if the existing value did not match the comparison value.</returns>
        public bool TryUpdate(TKey key, TValue value, TValue comparisonValue) => dictionary.TryUpdate(key, value, comparisonValue);
        /// <summary>
        /// Attempts to remove and return the value with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to remove.</param>
        /// <param name="value">When this method returns, contains the removed value, if found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns>true if the key was found and removed; otherwise, false.</returns>
        public bool TryRemove(TKey key,
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out TValue value) => dictionary.TryRemove(key, out value);
    }
}