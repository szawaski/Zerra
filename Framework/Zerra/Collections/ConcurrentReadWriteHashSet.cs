// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        public int Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    var count = hashSet.Count;
                    return count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            locker.EnterWriteLock();
            try
            {
                var add = hashSet.Add(item);
                return add;
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
                hashSet.Clear();
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
                var contains = hashSet.Contains(item);
                return contains;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public void CopyTo(T[] array)
        {
            locker.EnterReadLock();
            try
            {
                hashSet.CopyTo(array);
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
                hashSet.CopyTo(array, arrayIndex);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (count < 0 || arrayIndex + count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            locker.EnterReadLock();
            try
            {
                hashSet.CopyTo(array, arrayIndex, count);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

#if !NETSTANDARD2_0
        public int EnsureCapacity(int capacity)
        {
            locker.EnterWriteLock();
            try
            {
                var result = hashSet.EnsureCapacity(capacity);
                return result;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
#endif
        public void ExceptWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            try
            {
                hashSet.ExceptWith(other);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            try
            {
                hashSet.IntersectWith(other);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            try
            {
                var isProperSubsetOf = hashSet.IsProperSubsetOf(other);
                return isProperSubsetOf;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            try
            {
                var isProperSupersetOf = hashSet.IsProperSupersetOf(other);
                return isProperSupersetOf;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            try
            {
                var isSubsetOf = hashSet.IsSubsetOf(other);
                return isSubsetOf;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            try
            {
                var isSupersetOf = hashSet.IsSupersetOf(other);
                return isSupersetOf;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            try
            {
                var overlaps = hashSet.Overlaps(other);
                return overlaps;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            try
            {
                var removed = hashSet.Remove(item);
                return removed;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public int RemoveWhere(Predicate<T> match)
        {
            locker.EnterWriteLock();
            try
            {
                var removed = hashSet.RemoveWhere(match);
                return removed;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            locker.EnterReadLock();
            try
            {
                var setEquals = hashSet.SetEquals(other);
                return setEquals;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            try
            {
                hashSet.SymmetricExceptWith(other);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
        public void TrimExcess()
        {
            locker.EnterWriteLock();
            try
            {
                hashSet.TrimExcess();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
#if !NETSTANDARD2_0
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            locker.EnterReadLock();
            try
            {
                var result = hashSet.TryGetValue(equalValue, out var tryActualValue);
                if (result)
                    actualValue = tryActualValue;
                else
                    actualValue = default;
                return result;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }
#endif
        public void UnionWith(IEnumerable<T> other)
        {
            locker.EnterWriteLock();
            try
            {
                hashSet.UnionWith(other);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        void ICollection<T>.Add(T item)
        {
            locker.EnterWriteLock();
            try
            {
                _ = hashSet.Add(item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            locker.EnterReadLock();
            try
            {
                IEnumerable<T> items = hashSet.ToArray();
                return items.GetEnumerator();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            locker.EnterReadLock();
            try
            {
                IEnumerable<T> items = hashSet.ToArray();
                return items.GetEnumerator();
            }
            finally
            {
                locker.ExitReadLock();
            }
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
