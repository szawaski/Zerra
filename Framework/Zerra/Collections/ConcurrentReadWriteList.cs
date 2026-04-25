// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic list implementation with read-write locking for optimized concurrent access.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class ConcurrentReadWriteList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList, IDisposable
    {
        private readonly ReaderWriterLockSlim locker = new(LockRecursionPolicy.NoRecursion);
        private readonly List<T> list = new();

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public T this[int index]
        {
            get
            {
                locker.EnterReadLock();
                if (index < 0 || index > list.Count - 1)
                {
                    locker.ExitReadLock();
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                var value = list[index];
                locker.ExitReadLock();
                return value;
            }
            set
            {
                locker.EnterWriteLock();
                if (index < 0 || index > list.Count - 1)
                {
                    locker.ExitWriteLock();
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                list[index] = value;
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public int Count
        {
            get
            {
                locker.EnterReadLock();
                var count = list.Count;
                locker.ExitReadLock();
                return count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the list is read-only. Always returns false.
        /// </summary>
        public bool IsReadOnly => false;

        int ICollection.Count => list.Count;
        bool ICollection.IsSynchronized => ((ICollection)list).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)list).IsSynchronized;
        void ICollection.CopyTo(Array array, int index) => ((ICollection)list).CopyTo(array, index);

        bool IList.IsFixedSize => ((IList)list).IsFixedSize;
        bool IList.IsReadOnly => ((IList)list).IsReadOnly;
        object? IList.this[int index]
        {
            get
            {
                locker.EnterReadLock();
                if (index < 0 || index > list.Count - 1)
                {
                    locker.ExitReadLock();
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                var value = list[index];
                locker.ExitReadLock();
                return value;
            }
            set
            {
                locker.EnterWriteLock();
                if (index < 0 || index > list.Count - 1)
                {
                    locker.ExitWriteLock();
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                if (value is not T casted)
                {
                    locker.ExitWriteLock();
                    throw new InvalidOperationException("value cannot be casted to the List type");
                }
                list[index] = casted;
                locker.ExitWriteLock();
            }
        }

        int IList.Add(object? value)
        {
            locker.EnterWriteLock();
            var result = ((IList)list).Add(value);
            locker.ExitWriteLock();
            return result;
        }
        void IList.Clear() => Clear();
        bool IList.Contains(object? value)
        {
            if (value is not T casted)
                throw new InvalidOperationException("value cannot be casted to the List type");
            return Contains(casted);
        }
        int IList.IndexOf(object? value)
        {
            if (value is not T casted)
                throw new InvalidOperationException("value cannot be casted to the List type");
            return IndexOf(casted);
        }
        void IList.Insert(int index, object? value)
        {
            if (value is not T casted)
                throw new InvalidOperationException("value cannot be casted to the List type");
            Insert(index, casted);
        }
        void IList.Remove(object? value)
        {
            if (value is not T casted)
                throw new InvalidOperationException("value cannot be casted to the List type");
            _ = Remove(casted);
        }
        void IList.RemoveAt(int index) => RemoveAt(index);

        /// <summary>
        /// Adds an element to the end of the list.
        /// </summary>
        /// <param name="item">The element to add.</param>
        public void Add(T item)
        {
            locker.EnterWriteLock();
            list.Add(item);
            locker.ExitWriteLock();
        }

        /// <summary>
        /// Adds all elements from the specified collection to the end of the list.
        /// </summary>
        /// <param name="items">The collection of elements to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            locker.EnterWriteLock();
            foreach (var item in items)
                list.Add(item);
            locker.ExitWriteLock();
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        public void Clear()
        {
            locker.EnterWriteLock();
            list.Clear();
            locker.ExitWriteLock();
        }

        /// <summary>
        /// Determines whether the list contains a specific element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public bool Contains(T item)
        {
            locker.EnterReadLock();
            var contains = list.Contains(item);
            locker.ExitReadLock();
            return contains;
        }

        /// <summary>
        /// Copies the elements of the list to an array, starting at a specified index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when arrayIndex is out of range.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            locker.EnterReadLock();
            list.CopyTo(array, arrayIndex);
            locker.ExitReadLock();
        }

        /// <summary>
        /// Creates a copy of the list as an array.
        /// </summary>
        /// <returns>An array containing all elements from the list.</returns>
        public T[] ToArray()
        {
            locker.EnterReadLock();
            var items = list.ToArray();
            locker.ExitReadLock();
            return items;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An enumerator for the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            locker.EnterReadLock();
            IEnumerable<T> items = list.ToArray();
            locker.ExitReadLock();
            return items.GetEnumerator();
        }

        /// <summary>
        /// Searches for the specified element and returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the element, or -1 if not found.</returns>
        public int IndexOf(T item)
        {
            locker.EnterReadLock();
            var index = list.IndexOf(item);
            locker.ExitReadLock();
            return index;
        }

        /// <summary>
        /// Inserts an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="item">The element to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void Insert(int index, T item)
        {
            locker.EnterWriteLock();
            if (index < 0 || index > list.Count - 1)
            {
                locker.ExitWriteLock();
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            list.Insert(index, item);
            locker.ExitWriteLock();
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the list.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was found and removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            var removed = list.Remove(item);
            locker.ExitWriteLock();
            return removed;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void RemoveAt(int index)
        {
            locker.EnterWriteLock();
            if (index < 0 || index > list.Count - 1)
            {
                locker.ExitWriteLock();
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            list.RemoveAt(index);
            locker.ExitWriteLock();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An enumerator for the list.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Releases all resources used by the list.
        /// </summary>
        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer to ensure proper cleanup of resources.
        /// </summary>
        ~ConcurrentReadWriteList()
        {
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            locker.Dispose();
        }
    }
}
