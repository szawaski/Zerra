// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;

namespace Zerra.Serialization.Json.Converters.Collections
{
    internal sealed class ArrayOrListAccessor<T>
    {
        public int Index;
        public int Length => array?.Length ?? list?.Count ?? throw new InvalidOperationException();

        private readonly bool IsArray;
        private readonly T[]? array;
        private readonly List<T>? list;

        public ArrayOrListAccessor(T[] array)
        {
            this.IsArray = true;
            this.array = array;
            this.list = null;
            this.Index = 0;
        }
        public ArrayOrListAccessor()
        {
            this.IsArray = false;
            this.array = null;
            this.list = new();
            this.Index = 0;
        }

        public T Get()
        {
            if (IsArray)
                return array![Index];
            else
                return list![Index];
        }

        public void Add(T value)
        {
            if (IsArray)
                array![Index++] = value;
            else
                list!.Add(value);
        }

        public T[] ToArray()
        {
            if (IsArray)
                return array!;
            else
                return list!.ToArray();
        }

        public List<T> ToList()
        {
            if (IsArray)
                return array!.ToList();
            else
                return list!;
        }
    }
}