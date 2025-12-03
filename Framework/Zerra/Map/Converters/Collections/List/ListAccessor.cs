// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Map
{
    internal sealed class ListAccessor<T>
    {
        public int Index;

        private readonly IList<T> list;
        private readonly bool hasExistingValues;
        public IList<T> List => list;

        public ListAccessor(IList<T> list)
        {
            this.list = list;
            this.hasExistingValues = list.Count > 0;
            Index = 0;
        }

        public T? Get()
        {
            if (!hasExistingValues)
                return default;
            return list[Index];
        }

        public void Set(T value)
        {
            if (hasExistingValues)
                list[Index] = value;
            else
                list.Add(value);
        }
    }
}