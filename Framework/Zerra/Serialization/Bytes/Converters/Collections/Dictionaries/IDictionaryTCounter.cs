// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class IDictionaryTCounter<TKey, TValue> : IDictionary<TKey, TValue>
          where TKey : notnull
    {
        private int count;
        public int Count => count;

        public TValue this[TKey key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Add(TKey key, TValue value)
        {
            count++;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            count++;
        }

        public ICollection<TKey> Keys => throw new NotImplementedException();
        public ICollection<TValue> Values => throw new NotImplementedException();
        public bool IsReadOnly => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();
        public bool Contains(KeyValuePair<TKey, TValue> item) => throw new NotImplementedException();
        public bool ContainsKey(TKey key) => throw new NotImplementedException();
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => throw new NotImplementedException();
        public bool Remove(TKey key) => throw new NotImplementedException();
        public bool Remove(KeyValuePair<TKey, TValue> item) => throw new NotImplementedException();
#if NET6_0_OR_GREATER
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => throw new NotImplementedException();
#else
        public bool TryGetValue(TKey key, out TValue value) => throw new NotImplementedException();
#endif
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}