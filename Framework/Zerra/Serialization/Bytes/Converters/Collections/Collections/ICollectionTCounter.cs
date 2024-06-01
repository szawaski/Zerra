// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zerra.Serialization.Bytes.Converters.Collections.Collections
{
    internal sealed class ICollectionTCounter<T> : ICollection<T>
    {
        private int count;
        public int Count => count;

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item)
        {
            count++;
        }

        public void Clear()=>throw new NotImplementedException();
        public bool Contains(T item) => throw new NotImplementedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        public bool Remove(T item) => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}