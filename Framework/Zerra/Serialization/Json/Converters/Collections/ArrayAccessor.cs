// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;

namespace Zerra.Serialization.Json.Converters.Collections
{
    internal sealed class ArrayAccessor<T>
    {
        public int Index;

        private readonly T[]? array;
        private readonly List<T>? list;

        public ArrayAccessor(T[] array)
        {
            this.array = array;
            list = null;
            Index = 0;
        }
        public ArrayAccessor()
        {
            array = null;
            list = new();
            Index = 0;
        }

        public T Get()
        {
            if (array is not null)
                return array[Index];
            if (list is not null)
                return list[Index];
            throw new InvalidOperationException();
        }

        public void Set(T value)
        {
            if (array is not null)
                array[Index] = value;
            if (list is not null)
                list.Add(value);
        }

        public T[] ToArray()
        {
            if (array is not null)
                return array;
            if (list is not null)
                return list.ToArray();
            throw new InvalidOperationException();
        }
    }
}