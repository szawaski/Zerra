// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    internal sealed class ArrayAccessor<T>
    {
        public int Index;

        private readonly T[] array;
        public T[] Array => array;

        public ArrayAccessor(T[] array)
        {
            this.array = array;
            Index = 0;
        }

        public T Get() => array[Index];

        public void Set(T value) => array[Index] = value;
    }
}