// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic list
    /// </summary>
    public class ConcurrentReadWriteList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList, IDisposable
    {
        private readonly ReaderWriterLockSlim locker = new(LockRecursionPolicy.NoRecursion);
        private readonly List<T> list = new();

        public T this[int index]
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    var value = list[index];
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
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    list[index] = value;
                }
                finally
                {
                    locker.ExitWriteLock();
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
                    var count = list.Count;
                    return count;
                }
                finally
                {
                    locker.ExitReadLock();
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
                locker.EnterReadLock();
                try
                {
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    var value = list[index];
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
                    if (index < 0 || index > list.Count - 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    if (value is not T casted)
                    {
                        throw new InvalidOperationException("value cannot be casted to the List type");
                    }
                    list[index] = casted;
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
                var result = ((IList)list).Add(value);
                return result;
            }
            finally
            {
                locker.ExitWriteLock();
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

        public void AddRange(IEnumerable<T> items)
        {
            locker.EnterWriteLock();
            try
            {
                foreach (var item in items)
                    list.Add(item);
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
                list.Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            locker.EnterReadLock();
            try
            {
                var contains = list.Contains(item);
                return contains;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

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

        public T[] ToArray()
        {
            locker.EnterReadLock();
            try
            {
                var items = list.ToArray();
                return items;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            locker.EnterReadLock();
            try
            {
                IEnumerable<T> items = list.ToArray();
                return items.GetEnumerator();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public int IndexOf(T item)
        {
            locker.EnterReadLock();
            try
            {
                var index = list.IndexOf(item);
                return index;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public void Insert(int index, T item)
        {
            locker.EnterWriteLock();
            try
            {
                if (index < 0 || index > list.Count - 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                list.Insert(index, item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            try
            {
                var removed = list.Remove(item);
                return removed;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public void RemoveAt(int index)
        {
            locker.EnterWriteLock();
            try
            {
                if (index < 0 || index > list.Count - 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
                list.RemoveAt(index);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }
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
