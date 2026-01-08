// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Collections
{
    /// <summary>
    /// Thread safe generic hash set implementation with full set operations support.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    public class ConcurrentHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ISet<T>
    {
        private readonly Lock locker = new();
        private readonly HashSet<T> hashSet = new();

        /// <summary>
        /// Gets the number of elements in the set.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the set is read-only. Always returns false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an element to the set.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <returns>True if the element was added; false if it already exists.</returns>
        public bool Add(T item)
        {
            lock (locker)
            {
                var add = hashSet.Add(item);
                return add;
            }
        }

        /// <summary>
        /// Removes all elements from the set.
        /// </summary>
        public void Clear()
        {
            lock (locker)
            {
                hashSet.Clear();
            }
        }

        /// <summary>
        /// Determines whether the set contains a specific element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public bool Contains(T item)
        {
            lock (locker)
            {
                var contains = hashSet.Contains(item);
                return contains;
            }
        }

        /// <summary>
        /// Copies the elements of the set to an array, starting at the beginning.
        /// </summary>
        /// <param name="array">The destination array.</param>
        public void CopyTo(T[] array)
        {
            lock (locker)
            {
                hashSet.CopyTo(array);
            }
        }

        /// <summary>
        /// Copies the elements of the set to an array, starting at a specified index.
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
                hashSet.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Copies a specified number of elements from the set to an array, starting at a specified index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when arrayIndex or count is out of range.</exception>
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
        /// <summary>
        /// Ensures that the set has capacity for the specified number of elements.
        /// </summary>
        /// <param name="capacity">The minimum capacity required.</param>
        /// <returns>The new capacity of the set.</returns>
        public int EnsureCapacity(int capacity)
        {
            lock (locker)
            {
                var result = hashSet.EnsureCapacity(capacity);
                return result;
            }
        }
#endif

        /// <summary>
        /// Removes all elements in the specified collection from the set.
        /// </summary>
        /// <param name="other">The collection of elements to remove.</param>
        public void ExceptWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.ExceptWith(other);
            }
        }

        /// <summary>
        /// Modifies the set to contain only elements that are also in the specified collection.
        /// </summary>
        /// <param name="other">The collection to intersect with.</param>
        public void IntersectWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.IntersectWith(other);
            }
        }

        /// <summary>
        /// Determines whether the set is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare with.</param>
        /// <returns>True if the set is a proper subset; otherwise, false.</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isProperSubsetOf = hashSet.IsProperSubsetOf(other);
                return isProperSubsetOf;
            }
        }

        /// <summary>
        /// Determines whether the set is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare with.</param>
        /// <returns>True if the set is a proper superset; otherwise, false.</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isProperSupersetOf = hashSet.IsProperSupersetOf(other);
                return isProperSupersetOf;
            }
        }

        /// <summary>
        /// Determines whether the set is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare with.</param>
        /// <returns>True if the set is a subset; otherwise, false.</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isSubsetOf = hashSet.IsSubsetOf(other);
                return isSubsetOf;
            }
        }

        /// <summary>
        /// Determines whether the set is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare with.</param>
        /// <returns>True if the set is a superset; otherwise, false.</returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            lock (locker)
            {
                var isSupersetOf = hashSet.IsSupersetOf(other);
                return isSupersetOf;
            }
        }

        /// <summary>
        /// Determines whether the set shares any elements with the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare with.</param>
        /// <returns>True if the set overlaps with the collection; otherwise, false.</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            lock (locker)
            {
                var overlaps = hashSet.Overlaps(other);
                return overlaps;
            }
        }

        /// <summary>
        /// Removes a specific element from the set.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was found and removed; otherwise, false.</returns>
        public bool Remove(T item)
        {
            lock (locker)
            {
                var removed = hashSet.Remove(item);
                return removed;
            }
        }

        /// <summary>
        /// Removes all elements that match the specified predicate.
        /// </summary>
        /// <param name="match">The predicate that defines which elements to remove.</param>
        /// <returns>The number of elements removed.</returns>
        public int RemoveWhere(Predicate<T> match)
        {
            lock (locker)
            {
                var removed = hashSet.RemoveWhere(match);                
                return removed;
            }
        }

        /// <summary>
        /// Determines whether the set and the specified collection contain the same elements.
        /// </summary>
        /// <param name="other">The collection to compare with.</param>
        /// <returns>True if the sets are equal; otherwise, false.</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            lock (locker)
            {
                var setEquals = hashSet.SetEquals(other);
                return setEquals;
            }
        }

        /// <summary>
        /// Modifies the set to contain only elements that are in either this set or the specified collection, but not both.
        /// </summary>
        /// <param name="other">The collection to use for the symmetric difference.</param>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.SymmetricExceptWith(other);
            }
        }

        /// <summary>
        /// Removes unused capacity from the set.
        /// </summary>
        public void TrimExcess()
        {
            lock (locker)
            {
                hashSet.TrimExcess();
            }
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Searches for an element that matches the value and retrieves the actual value from the set.
        /// </summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The actual value in the set if found; otherwise, the default value.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
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

        /// <summary>
        /// Modifies the set to contain the union of itself and the specified collection.
        /// </summary>
        /// <param name="other">The collection to union with.</param>
        public void UnionWith(IEnumerable<T> other)
        {
            lock (locker)
            {
                hashSet.UnionWith(other);
            }
        }

        /// <summary>
        /// Adds an element to the set. This is the explicit implementation of ICollection&lt;T&gt;.Add.
        /// </summary>
        /// <param name="item">The element to add.</param>
        void ICollection<T>.Add(T item)
        {
            lock (locker)
            {
                _ = hashSet.Add(item);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set.
        /// </summary>
        /// <returns>An enumerator for the set.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            lock (locker)
            {
                IEnumerable<T> items = hashSet.ToArray();
                return items.GetEnumerator();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the set.
        /// </summary>
        /// <returns>An enumerator for the set.</returns>
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
