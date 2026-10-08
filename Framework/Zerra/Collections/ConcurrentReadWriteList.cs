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
                try
                {
                    return list[index];
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
                    list[index] = value;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
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
                try
                {
                    return list.Count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the list is read-only. Always returns false.
        /// </summary>
        public bool IsReadOnly => false;

        int ICollection.Count => Count;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw new NotSupportedException($"{nameof(ICollection.SyncRoot)} is not supported, the list locks internally");
        void ICollection.CopyTo(Array array, int index)
        {
            locker.EnterReadLock();
            try
            {
                ((ICollection)list).CopyTo(array, index);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => false;
        object? IList.this[int index]
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    return list[index];
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
                    ((IList)list)[index] = value;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }

        int IList.Add(object? value)
        {
            locker.EnterWriteLock();
            try
            {
                return ((IList)list).Add(value);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        void IList.Clear() => Clear();
        bool IList.Contains(object? value)
        {
            locker.EnterReadLock();
            try
            {
                return ((IList)list).Contains(value);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        int IList.IndexOf(object? value)
        {
            locker.EnterReadLock();
            try
            {
                return ((IList)list).IndexOf(value);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        void IList.Insert(int index, object? value)
        {
            locker.EnterWriteLock();
            try
            {
                ((IList)list).Insert(index, value);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        void IList.Remove(object? value)
        {
            locker.EnterWriteLock();
            try
            {
                ((IList)list).Remove(value);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        void IList.RemoveAt(int index) => RemoveAt(index);

        /// <summary>
        /// Adds an element to the end of the list.
        /// </summary>
        /// <param name="item">The element to add.</param>
        public void Add(T item)
        {
            locker.EnterWriteLock();
            try
            {
                list.Add(item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds all elements from the specified collection to the end of the list.
        /// </summary>
        /// <param name="items">The collection of elements to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            locker.EnterWriteLock();
            try
            {
                list.AddRange(items);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        public void Clear()
        {
            locker.EnterWriteLock();
            try
            {
                list.Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Determines whether the list contains a specific element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public bool Contains(T item)
        {
            locker.EnterReadLock();
            try
            {
                return list.Contains(item);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies the elements of the list to an array, starting at a specified index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when arrayIndex is out of range.</exception>
        /// <exception cref="ArgumentException">Thrown when the array is too small.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            locker.EnterReadLock();
            try
            {
                list.CopyTo(array, arrayIndex);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Creates a copy of the list as an array.
        /// </summary>
        /// <returns>An array containing all elements from the list.</returns>
        public T[] ToArray()
        {
            locker.EnterReadLock();
            try
            {
                return list.ToArray();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a snapshot of the list.
        /// </summary>
        /// <returns>An enumerator for the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable<T> items;
            locker.EnterReadLock();
            try
            {
                items = list.ToArray();
            }
            finally
            {
                locker.ExitReadLock();
            }
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
            try
            {
                return list.IndexOf(item);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Inserts an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted. Equal to the count adds to the end.</param>
        /// <param name="item">The element to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void Insert(int index, T item)
        {
            locker.EnterWriteLock();
            try
            {
                list.Insert(index, item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the list.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was found and removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            try
            {
                return list.Remove(item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void RemoveAt(int index)
        {
            locker.EnterWriteLock();
            try
            {
                list.RemoveAt(index);
            }
            finally
            {
                locker.ExitWriteLock();
            }
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