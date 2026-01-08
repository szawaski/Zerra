using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Collections
{
    /// <summary>
    /// A read-only stack collection that allows peeking and popping elements without adding new ones.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stack.</typeparam>
    public class ReadOnlyStack<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
    {
        private const int downsizeEveryCount = 16;
        private T[] stack;
        private int index;

        /// <summary>
        /// Initializes a new instance of the ReadOnlyStack class with elements from the specified collection.
        /// </summary>
        /// <param name="values">The collection of elements to initialize the stack with.</param>
        public ReadOnlyStack(IEnumerable<T> values)
        {
            this.stack = values.ToArray();
            this.index = 0;
        }

        /// <summary>
        /// Initializes a new instance of the ReadOnlyStack class with a single element.
        /// </summary>
        /// <param name="value">The element to initialize the stack with.</param>
        public ReadOnlyStack(T value)
        {
            this.stack = [value];
            this.index = 0;
        }

        /// <summary>
        /// Gets the number of elements in the stack.
        /// </summary>
        public int Count => stack.Length - index;

        bool ICollection.IsSynchronized => false;

        private readonly object syncRoot = new();
        object ICollection.SyncRoot => syncRoot;

        /// <summary>
        /// Determines whether the stack contains a specific element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public bool Contains(T item)
        {
            return stack.Skip(index).Contains(item);
        }

        /// <summary>
        /// Copies the elements of the stack to an array, starting at a specified index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
        /// <exception cref="ArgumentException">Thrown when there is not enough space in the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            var length = stack.Length - index;
            if (length > array.Length - arrayIndex)
                throw new ArgumentException($"The number of elements in the source {nameof(ReadOnlyStack<T>)} is greater than the available space from arrayIndex to the end of the destination array.");
            Array.Copy(stack, index, array, arrayIndex, length);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            var length = stack.Length - index;
            if (length > array.Length - arrayIndex)
                throw new ArgumentException($"The number of elements in the source {nameof(ReadOnlyStack<T>)} is greater than the available space from arrayIndex to the end of the destination array.");
            Array.Copy(stack, index, array, arrayIndex, length);
        }

        /// <summary>
        /// Returns the element at the top of the stack without removing it.
        /// </summary>
        /// <returns>The element at the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public T Peek()
        {
            if (index == stack.Length)
                throw new InvalidOperationException($"{nameof(ReadOnlyStack<T>)} is empty");
            return stack[index];
        }

        /// <summary>
        /// Removes and returns the element at the top of the stack.
        /// </summary>
        /// <returns>The element removed from the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public T Pop()
        {
            if (index == stack.Length)
                throw new InvalidOperationException($"{nameof(ReadOnlyStack<T>)} is empty");
            var result = stack[index++];
            Downsize();
            return result;
        }

        /// <summary>
        /// Creates a copy of the stack as an array.
        /// </summary>
        /// <returns>An array containing all elements from the stack.</returns>
        public T[] ToArray()
        {
            return stack.Skip(index).ToArray();
        }

        /// <summary>
        /// Attempts to return the element at the top of the stack without removing it.
        /// </summary>
        /// <param name="result">The element at the top of the stack if the stack is not empty; otherwise, the default value.</param>
        /// <returns>True if the stack is not empty; otherwise, false.</returns>
        public bool TryPeek(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out T result)
        {
            if (index == stack.Length)
            {
                result = default;
                return false;
            }
            result = stack[index];
            return true;
        }

        /// <summary>
        /// Attempts to remove and return the element at the top of the stack.
        /// </summary>
        /// <param name="result">The element removed from the top of the stack if the stack is not empty; otherwise, the default value.</param>
        /// <returns>True if the stack is not empty; otherwise, false.</returns>
        public bool TryPop(
#if !NETSTANDARD2_0
            [MaybeNullWhen(false)]
#endif
        out T result)
        {
            if (index == stack.Length)
            {
                result = default;
                return false;
            }
            result = stack[index++];
            Downsize();
            return true;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the stack.
        /// </summary>
        /// <returns>An enumerator for the stack.</returns>
        public IEnumerator GetEnumerator()
        {
            return stack.Skip(index).GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the stack.
        /// </summary>
        /// <returns>An enumerator for the stack.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return stack.Skip(index).GetEnumerator();
        }

        private void Downsize()
        {
            if (index >= downsizeEveryCount)
            {
                var newStack = new T[stack.Length - index];
                Array.Copy(stack, index, newStack, 0, newStack.Length);
                stack = newStack;
                index = 0;
            }
        }
    }
}
