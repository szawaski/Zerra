// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.Collections;

namespace Zerra.Test.Collections
{
    public class ReadOnlyStackTest
    {
        [Fact]
        public void Constructor_WithValue()
        {
            var stack = new ReadOnlyStack<int>(42);
            _ = Assert.Single(stack);
            Assert.Equal(42, stack.Peek());
        }

        [Fact]
        public void Constructor_WithEnumerable()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3, 4, 5 });
            Assert.Equal(5, stack.Count);
            Assert.Equal(1, stack.Peek());
        }

        [Fact]
        public void Count_Property()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Assert.Equal(3, stack.Count);
            _ = stack.Pop();
            Assert.Equal(2, stack.Count);
        }

        [Fact]
        public void Peek_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Assert.Equal(1, stack.Peek());
            Assert.Equal(1, stack.Peek());
            Assert.Equal(3, stack.Count);
        }

        [Fact]
        public void Peek_Empty_Throws()
        {
            var stack = new ReadOnlyStack<int>(new int[0]);
            _ = Assert.Throws<InvalidOperationException>(() => stack.Peek());
        }

        [Fact]
        public void Pop_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Assert.Equal(1, stack.Pop());
            Assert.Equal(2, stack.Count);
            Assert.Equal(2, stack.Pop());
            _ = Assert.Single(stack);
            Assert.Equal(3, stack.Pop());
            Assert.Empty(stack);
        }

        [Fact]
        public void Pop_Empty_Throws()
        {
            var stack = new ReadOnlyStack<int>(new int[0]);
            _ = Assert.Throws<InvalidOperationException>(() => stack.Pop());
        }

        [Fact]
        public void TryPeek_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Assert.True(stack.TryPeek(out var result));
            Assert.Equal(1, result);
        }

        [Fact]
        public void TryPeek_Empty()
        {
            var stack = new ReadOnlyStack<int>(new int[0]);
            Assert.False(stack.TryPeek(out var result));
            Assert.Equal(0, result);
        }

        [Fact]
        public void TryPop_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Assert.True(stack.TryPop(out var result));
            Assert.Equal(1, result);
            Assert.Equal(2, stack.Count);
        }

        [Fact]
        public void TryPop_Empty()
        {
            var stack = new ReadOnlyStack<int>(new int[0]);
            Assert.False(stack.TryPop(out var result));
            Assert.Equal(0, result);
        }

        [Fact]
        public void Contains_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Assert.True(stack.Contains(1));
            Assert.True(stack.Contains(2));
            Assert.False(stack.Contains(4));
        }

        [Fact]
        public void Contains_AfterPop()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            _ = stack.Pop();
            Assert.False(stack.Contains(1));
            Assert.True(stack.Contains(2));
        }

        [Fact]
        public void CopyTo_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            var array = new int[3];
            stack.CopyTo(array, 0);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);
        }

        [Fact]
        public void CopyTo_WithOffset()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            var array = new int[5];
            stack.CopyTo(array, 1);
            Assert.Equal(0, array[0]);
            Assert.Equal(1, array[1]);
            Assert.Equal(2, array[2]);
            Assert.Equal(3, array[3]);
            Assert.Equal(0, array[4]);
        }

        [Fact]
        public void ICollection_CopyTo_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            Array array = new int[3];
            ((System.Collections.ICollection)stack).CopyTo(array, 0);
            Assert.Equal(1, (int)array.GetValue(0)!);
            Assert.Equal(2, (int)array.GetValue(1)!);
            Assert.Equal(3, (int)array.GetValue(2)!);
        }

        [Fact]
        public void ICollection_CopyTo_WithOffset()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            var array = new int[5];
            ((System.Collections.ICollection)stack).CopyTo(array, 1);
            Assert.Equal(0, array[0]);
            Assert.Equal(1, array[1]);
            Assert.Equal(2, array[2]);
            Assert.Equal(3, array[3]);
            Assert.Equal(0, array[4]);
        }

        [Fact]
        public void CopyTo_InsufficientSpace_Throws()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            var array = new int[2];
            _ = Assert.Throws<ArgumentException>(() => stack.CopyTo(array, 0));
        }

        [Fact]
        public void ToArray_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            var array = stack.ToArray();
            Assert.Equal(3, array.Length);
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);
        }

        [Fact]
        public void ToArray_AfterPop()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            _ = stack.Pop();
            var array = stack.ToArray();
            Assert.Equal(2, array.Length);
            Assert.Equal(2, array[0]);
            Assert.Equal(3, array[1]);
        }

        [Fact]
        public void GetEnumerator_Method()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3 });
            var items = stack.ToList();
            Assert.Equal(3, items.Count);
            Assert.Equal(1, items[0]);
            Assert.Equal(2, items[1]);
            Assert.Equal(3, items[2]);
        }

        [Fact]
        public void GetEnumerator_AfterPops()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3, 4, 5 });
            _ = stack.Pop();
            _ = stack.Pop();
            var items = stack.ToList();
            Assert.Equal(3, items.Count);
            Assert.Equal(3, items[0]);
            Assert.Equal(4, items[1]);
            Assert.Equal(5, items[2]);
        }

        [Fact]
        public void Downsize_AfterMultiplePops()
        {
            var stack = new ReadOnlyStack<int>(Enumerable.Range(1, 20).ToArray());
            Assert.Equal(20, stack.Count);

            for (int i = 0; i < 16; i++)
                _ = stack.Pop();

            Assert.Equal(4, stack.Count);
            var remaining = stack.ToArray();
            Assert.Equal(4, remaining.Length);
        }

        [Fact]
        public void MultiplePopSequence()
        {
            var stack = new ReadOnlyStack<int>(new[] { 1, 2, 3, 4, 5 });
            var values = new List<int>();

            while (stack.TryPop(out var value))
                values.Add(value);

            Assert.Equal(5, values.Count);
            Assert.Equal(1, values[0]);
            Assert.Equal(5, values[4]);
        }
    }
}
