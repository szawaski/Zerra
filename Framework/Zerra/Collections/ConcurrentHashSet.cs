// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic hash set
    /// </summary>
    public class ConcurrentHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T>
    {
        private readonly object locker = new();
        private readonly HashSet<T> hashSet = new();

        public int Count
        {
            get
            {
                lock (locker)
                {
                    var count = hashSet.Count;
                    return count;
                }
            }
        }

        public bool IsReadOnly => false;

        public bool Add(T item)
        {
            lock (locker)
            {
                var add = hashSet.Add(item);
                return add;
            }
        }
        public void Clear()
        {
            lock (locker)
            {
                hashSet.Clear();
            }
        }
        public bool Contains(T item)
        {
            lock (locker)
            {
                var contains = hashSet.Contains(item);
                return contains;
            }
        }

        public void CopyTo(T[] array)
        {
            lock (locker)
            {
                hashSet.CopyTo(array);
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            lock (locker)
            {
                hashSet.CopyTo(array, arrayIndex);
            }
        }
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length - 1)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (count < 0 || arrayIndex + count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            lock (locker)
            {
                hashSet.CopyTo(array, arrayIndex, count);
            }
        }

#if !NETSTANDARD2_0
        public int EnsureCapacity(int capacity)
        {
            lock (locker)
            {
                var result = hashSet.EnsureCapacity(capacity);
                return result;
            }
        }
#endif
        public void ExceptWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.ExceptWith(other);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.IntersectWith(other);
            }
        }
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isProperSubsetOf = hashSet.IsProperSubsetOf(other);
                return isProperSubsetOf;
            }
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isProperSupersetOf = hashSet.IsProperSupersetOf(other);
                return isProperSupersetOf;
            }
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isSubsetOf = hashSet.IsSubsetOf(other);
                return isSubsetOf;
            }
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isSupersetOf = hashSet.IsSupersetOf(other);
                return isSupersetOf;
            }
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            lock (locker)
            {
                var overlaps = hashSet.Overlaps(other);
                return overlaps;
            }
        }
        public bool Remove(T item)
        {
            lock (locker)
            {
                var removed = hashSet.Remove(item);
                return removed;
            }
        }
        public int RemoveWhere(Predicate<T> match)
        {
            lock (locker)
            {
                var removed = hashSet.RemoveWhere(match);                
                return removed;
            }
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            lock (locker)
            {
                var setEquals = hashSet.SetEquals(other);
                return setEquals;
            }
        }
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.SymmetricExceptWith(other);
            }
        }
        public void TrimExcess()
        {
            lock (locker)
            {
                hashSet.TrimExcess();
            }
        }
#if !NETSTANDARD2_0
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            lock (locker)
            {
                var result = hashSet.TryGetValue(equalValue, out var tryActualValue);
                if (result)
                    actualValue = tryActualValue;
                else
                    actualValue = default;
                return result;
            }
        }
#endif
        public void UnionWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.UnionWith(other);
            }
        }

        void ICollection<T>.Add(T item)
        {
            lock (locker)
            {
                _ = hashSet.Add(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (locker)
            {
                IEnumerable<T> items = hashSet.ToArray();
                return items.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (locker)
            {
                IEnumerable<T> items = hashSet.ToArray();
                return items.GetEnumerator();
            }
        }
    }
}
