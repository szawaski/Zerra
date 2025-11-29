// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class IDictionaryCounter : IDictionary
    {
        private int count;
        public int Count => count;

        public void Add(object key, object? value)
        {
            count++;
        }

        public object? this[object key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsFixedSize => throw new NotImplementedException();
        public bool IsReadOnly => throw new NotImplementedException();
        public ICollection Keys => throw new NotImplementedException();
        public ICollection Values => throw new NotImplementedException();
        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(object key) => throw new NotImplementedException();
        public void CopyTo(Array array, int index) => throw new NotImplementedException();
        public IDictionaryEnumerator GetEnumerator() => throw new NotImplementedException();
        public void Remove(object key) => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}