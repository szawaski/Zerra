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
#if NETSTANDARD2_0
        private readonly object locker = new();
#else
        private readonly Lock locker = new();
#endif
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
                    return list[index];
                }
            }
            set
            {
                lock (locker)
                {
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
                    return list.Count;
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
            lock (locker)
            {
                ((ICollection)list).CopyTo(array, index);
            }
        }

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => false;
        object? IList.this[int index]
        {
            get
            {
                lock (locker)
                {
                    return list[index];
                }
            }
            set
            {
                lock (locker)
                {
                    ((IList)list)[index] = value;
                }
            }
        }

        int IList.Add(object? value)
        {
            lock (locker)
            {
                return ((IList)list).Add(value);
            }
        }
        void IList.Clear() => Clear();
        bool IList.Contains(object? value)
        {
            lock (locker)
            {
                return ((IList)list).Contains(value);
            }
        }
        int IList.IndexOf(object? value)
        {
            lock (locker)
            {
                return ((IList)list).IndexOf(value);
            }
        }
        void IList.Insert(int index, object? value)
        {
            lock (locker)
            {
                ((IList)list).Insert(index, value);
            }
        }
        void IList.Remove(object? value)
        {
            lock (locker)
            {
                ((IList)list).Remove(value);
            }
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
                list.AddRange(items);
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
                return list.Contains(item);
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
                return list.ToArray();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a snapshot of the list.
        /// </summary>
        /// <returns>An enumerator for the list.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable<T> items;
            lock (locker)
            {
                items = list.ToArray();
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
            lock (locker)
            {
                return list.IndexOf(item);
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
            lock (locker)
            {
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
                return list.Remove(item);
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