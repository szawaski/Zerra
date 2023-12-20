// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic list
    /// </summary>
    public class ConcurrentList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList
    {
        private readonly object locker = new();
        private readonly List<T> list = new();

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
            Remove(casted);
        }
        void IList.RemoveAt(int index) => RemoveAt(index);

        public void Add(T item)
        {
            lock (locker)
            {
                list.Add(item);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            lock (locker)
            {
                foreach (var item in items)
                    list.Add(item);
            }
        }

        public void Clear()
        {
            lock (locker)
            {
                list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (locker)
            {
                var contains = list.Contains(item);
                return contains;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            lock (locker)
            {
                list.CopyTo(array, arrayIndex);
            }
        }

        public T[] ToArray()
        {
            lock (locker)
            {
                var items = list.ToArray();
                return items;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (locker)
            {
                IEnumerable<T> items = list.ToArray();
                return items.GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (locker)
            {
                var index = list.IndexOf(item);
                return index;
            }
        }

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

        public bool Remove(T item)
        {
            lock (locker)
            {
                var removed = list.Remove(item);
                return removed;
            }
        }

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
