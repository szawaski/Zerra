// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic hash set
    /// </summary>
    public class ConcurrentReadWriteHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T>, IDisposable
    {
        private readonly ReaderWriterLockSlim locker = new(LockRecursionPolicy.NoRecursion);
        private readonly HashSet<T> hashSet = new();

        public int Count => hashSet.Count;

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            locker.EnterWriteLock();
            var add = hashSet.Add(item);
            locker.ExitWriteLock();
            return add;
        }
        public void Clear()
        {
            locker.EnterWriteLock();
            hashSet.Clear();
            locker.ExitWriteLock();
        }
        public bool Contains(T item)
        {
            locker.EnterReadLock();
            var contains = hashSet.Contains(item);
            locker.ExitReadLock();
            return contains;
        }

        public void CopyTo(T[] array)
        {
            locker.EnterReadLock();
            hashSet.CopyTo(array);
            locker.ExitReadLock();
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException("arrayIndex");

            locker.EnterReadLock();
            hashSet.CopyTo(array, arrayIndex);
            locker.ExitReadLock();
        }
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException("arrayIndex");

            if (count < 0 || arrayIndex + count > array.Length)
                throw new ArgumentOutOfRangeException("count");

            locker.EnterReadLock();
            hashSet.CopyTo(array, arrayIndex, count);
            locker.ExitReadLock();
        }

#if !NETSTANDARD2_0
        public int EnsureCapacity(int capacity)
        {
            locker.EnterWriteLock();
            var result = hashSet.EnsureCapacity(capacity);
            locker.ExitWriteLock();
            return result;
        }
#endif
        public void ExceptWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            hashSet.ExceptWith(other);
            locker.ExitWriteLock();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            hashSet.IntersectWith(other);
            locker.ExitWriteLock();
        }
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            var isProperSubsetOf = hashSet.IsProperSubsetOf(other);
            locker.ExitReadLock();
            return isProperSubsetOf;
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            var isProperSupersetOf = hashSet.IsProperSupersetOf(other);
            locker.ExitReadLock();
            return isProperSupersetOf;
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            var isSubsetOf = hashSet.IsSubsetOf(other);
            locker.ExitReadLock();
            return isSubsetOf;
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            var isSupersetOf = hashSet.IsSupersetOf(other);
            locker.ExitReadLock();
            return isSupersetOf;
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            var overlaps = hashSet.Overlaps(other);
            locker.ExitReadLock();
            return overlaps;
        }
        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            var removed = hashSet.Remove(item);
            locker.ExitWriteLock();
            return removed;
        }
        public int RemoveWhere(Predicate<T> match)
        {
            locker.EnterWriteLock();
            var removed = hashSet.RemoveWhere(match);
            locker.ExitWriteLock();
            return removed;
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            var setEquals = hashSet.SetEquals(other);
            locker.ExitReadLock();
            return setEquals;
        }
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            hashSet.SymmetricExceptWith(other);
            locker.ExitWriteLock();
        }
        public void TrimExcess()
        {
            locker.EnterWriteLock();
            hashSet.TrimExcess();
            locker.ExitWriteLock();
        }
#if !NETSTANDARD2_0
        public bool TryGetValue(T equalValue, out T actualValue)
        {
            locker.EnterReadLock();
            var result = hashSet.TryGetValue(equalValue, out var tryActualValue);
            if (result)
                actualValue = tryActualValue;
            else
                actualValue = default;
            locker.ExitReadLock();
            return result;
        }
#endif
        public void UnionWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            hashSet.UnionWith(other);
            locker.ExitWriteLock();
        }

        void ICollection<T>.Add(T item)
        {
            locker.EnterWriteLock();
            _ = hashSet.Add(item);
            locker.ExitWriteLock();
        }

        public IEnumerator<T> GetEnumerator()
        {
            locker.EnterReadLock();
            IEnumerable<T> items = hashSet.ToArray();
            locker.ExitReadLock();
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            locker.EnterReadLock();
            IEnumerable<T> items = hashSet.ToArray();
            locker.ExitReadLock();
            return items.GetEnumerator();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }
        ~ConcurrentReadWriteHashSet()
        {
            DisposeInternal();
        }

        private void DisposeInternal()
        {
            locker.Dispose();
        }
    }
}
