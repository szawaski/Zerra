using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.Collections
{
    public class ReadOnlyStack<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
    {
        private const int downsizeEveryCount = 16;
        private T[] stack;
        private int index;

        public ReadOnlyStack(IEnumerable<T> values)
        {
            this.stack = values.ToArray();
            this.index = 0;
        }
        public ReadOnlyStack(T value)
        {
            this.stack = new T[] { value };
            this.index = 0;
        }

        public int Count => stack.Length - index;

        bool ICollection.IsSynchronized => false;

        private readonly object syncRoot = new object();
        object ICollection.SyncRoot => syncRoot;

        public bool Contains(T item)
        {
            return stack.Skip(index).Contains(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            var length = stack.Length - index;
            if (length < array.Length - arrayIndex)
                throw new ArgumentException($"The number of elements in the source {nameof(ReadOnlyStack<T>)} is greater than the available space from arrayIndex to the end of the destination array.");
            Array.Copy(stack, index, array, arrayIndex, length);
        }

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            var length = stack.Length - index;
            if (length < array.Length - index)
                throw new ArgumentException($"The number of elements in the source {nameof(ReadOnlyStack<T>)} is greater than the available space from arrayIndex to the end of the destination array.");
            Array.Copy(stack, index, array, arrayIndex, length);
        }

        public T Peek()
        {
            if (index == stack.Length)
                throw new InvalidOperationException($"{nameof(ReadOnlyStack<T>)} is empty");
            return stack[index];
        }
        public T Pop()
        {
            if (index == stack.Length)
                throw new InvalidOperationException($"{nameof(ReadOnlyStack<T>)} is empty");
            var result = stack[index++];
            Downsize();
            return result;
        }

        public T[] ToArray()
        {
            return stack.Skip(index).ToArray();
        }

        public bool TryPeek(out T result)
        {
            if (index == stack.Length)
            {
                result = default;
                return false;
            }
            result = stack[index];
            return true;
        }
        public bool TryPop(out T result)
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

        public IEnumerator GetEnumerator()
        {
            return stack.Skip(index).GetEnumerator();
        }

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
