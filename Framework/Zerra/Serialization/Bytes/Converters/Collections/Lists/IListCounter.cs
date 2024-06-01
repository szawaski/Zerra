// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;

namespace Zerra.Serialization.Bytes.Converters.Collections.Lists
{
    internal sealed class IListCounter : IList
    {
        private int count;

        public int Count => count;
        public int Add(object? value)
        {
            return count++;
        }

        public object this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsFixedSize => throw new NotImplementedException();
        public bool IsReadOnly => throw new NotImplementedException();
        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(object? value) => throw new NotImplementedException();
        public void CopyTo(Array array, int index) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        public int IndexOf(object? value) => throw new NotImplementedException();
        public void Insert(int index, object? value) => throw new NotImplementedException();
        public void Remove(object? value) => throw new NotImplementedException();
        public void RemoveAt(int index) => throw new NotImplementedException();
    }
}