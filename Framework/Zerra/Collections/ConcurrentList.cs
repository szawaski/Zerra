// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic list implementation with full list operations support.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    public class ConcurrentList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList
    {
        private readonly Lock locker = new();
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
                lock (locker)
                {
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    var value = list[index];
                    return value;
                }
            }
            set
            {
                lock (locker)
                {
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    list[index] = value;
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
                lock (locker)
                {
                    var count = list.Count;
                    return count;
                }
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
                lock (locker)
                {
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    var value = list[index];
                    return value;
                }
            }
            set
            {
                lock (locker)
                {
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    if (value is not T casted)
                        throw new InvalidOperationException("value cannot be casted to the List type");
                    list[index] = casted;
                }
            }
        }

        int IList.Add(object? value)
        {
            lock (locker)
            {
                var result = ((IList)list).Add(value);
                return result;
            }
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
            lock (locker)
            {
                list.Add(item);
            }
        }

        /// <summary>
        /// Adds all elements from the specified collection to the end of the list.
        /// </summary>
        /// <param name="items">The collection of elements to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            lock (locker)
            {
                foreach (var item in items)
                    list.Add(item);
            }
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        public void Clear()
        {
            lock (locker)
            {
                list.Clear();
            }
        }

        /// <summary>
        /// Determines whether the list contains a specific element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public bool Contains(T item)
        {
            lock (locker)
            {
                var contains = list.Contains(item);
                return contains;
            }
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

            lock (locker)
            {
                list.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Creates a copy of the list as an array.
        /// </summary>
        /// <returns>An array containing all elements from the list.</returns>
        public T[] ToArray()
        {
            lock (locker)
            {
                var items = list.ToArray();
                return items;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An enumerator for the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            lock (locker)
            {
                IEnumerable<T> items = list.ToArray();
                return items.GetEnumerator();
            }
        }

        /// <summary>
        /// Searches for the specified element and returns the zero-based index of the first occurrence.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the element, or -1 if not found.</returns>
        public int IndexOf(T item)
        {
            lock (locker)
            {
                var index = list.IndexOf(item);
                return index;
            }
        }

        /// <summary>
        /// Inserts an element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="item">The element to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void Insert(int index, T item)
        {
            lock (locker)
            {
                if (index < 0 || index > list.Count - 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                list.Insert(index, item);
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the list.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was found and removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            lock (locker)
            {
                var removed = list.Remove(item);
                return removed;
            }
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
        public void RemoveAt(int index)
        {
            lock (locker)
            {
                if (index < 0 || index > list.Count - 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                list.RemoveAt(index);
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
    }
}
