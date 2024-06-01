// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;

namespace Zerra.Serialization.Bytes.Converters.Collections
{
    internal sealed class ArrayAccessor<T>
    {
        public int Index;
        public readonly int Length;

        private readonly T[]? array;
        public T[]? Array => array;

        public ArrayAccessor(T[] array)
        {
            this.array = array;
            Index = 0;
            Length = array.Length;
        }
        public ArrayAccessor(int length)
        {
            array = null;
            Index = 0;
            Length = length;
        }

        public T Get()
        {
            if (array == null)
                throw new InvalidOperationException();
            return array[Index];
        }

        public void Set(T value)
        {
            if (array == null)
                return;
            array[Index] = value;
        }
    }
}