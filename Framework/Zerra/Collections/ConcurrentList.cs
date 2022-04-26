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
    public class ConcurrentList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection, IList, IDisposable
    {
        private readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly List<T> list = new List<T>();

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

        public bool IsReadOnly => false;

        int ICollection.Count => list.Count;
        bool ICollection.IsSynchronized => ((ICollection)list).IsSynchronized;
        object ICollection.SyncRoot => ((ICollection)list).IsSynchronized;
        void ICollection.CopyTo(Array array, int index) => ((ICollection)list).CopyTo(array, index);

        bool IList.IsFixedSize => ((IList)list).IsFixedSize;
        bool IList.IsReadOnly => ((IList)list).IsReadOnly;
        object IList.this[int index]
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
                list[index] = (T)value;
                locker.ExitWriteLock();
            }
        }

        int IList.Add(object value)
        {
            locker.EnterWriteLock();
            var result = ((IList)list).Add(value);
            locker.ExitWriteLock();
            return result;
        }
        void IList.Clear() => Clear();
        bool IList.Contains(object value) => Contains((T)value);
        int IList.IndexOf(object value) => IndexOf((T)value);
        void IList.Insert(int index, object value) => Insert(index, (T)value);
        void IList.Remove(object value) => Remove((T)value);
        void IList.RemoveAt(int index) => RemoveAt(index);

        public void Add(T item)
        {
            locker.EnterWriteLock();
            list.Add(item);
            locker.ExitWriteLock();
        }

        public void AddRange(IEnumerable<T> items)
        {
            locker.EnterWriteLock();
            foreach (var item in items)
                list.Add(item);
            locker.ExitWriteLock();
        }

        public void Clear()
        {
            locker.EnterWriteLock();
            list.Clear();
            locker.ExitWriteLock();
        }

        public bool Contains(T item)
        {
            locker.EnterReadLock();
            var contains = list.Contains(item);
            locker.ExitReadLock();
            return contains;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            locker.EnterReadLock();
            list.CopyTo(array, arrayIndex);
            locker.ExitReadLock();
        }

        public T[] ToArray()
        {
            locker.EnterReadLock();
            var items = list.ToArray();
            locker.ExitReadLock();
            return items;
        }

        public IEnumerator<T> GetEnumerator()
        {
            locker.EnterReadLock();
            IEnumerable<T> items = list.ToArray();
            locker.ExitReadLock();
            return items.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            locker.EnterReadLock();
            var index = list.IndexOf(item);
            locker.ExitReadLock();
            return index;
        }

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

        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            var removed = list.Remove(item);
            locker.ExitWriteLock();
            return removed;
        }

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }
        ~ConcurrentList()
        {
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            locker.Dispose();
        }
    }
}
