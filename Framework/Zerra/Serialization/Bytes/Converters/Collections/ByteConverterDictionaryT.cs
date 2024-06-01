// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections
{
    internal sealed class ByteConverterDictionaryT<TParent, TDictionary, TKey, TValue> : ByteConverter<TParent, TDictionary>
        where TKey : notnull
    {
        private ByteConverter<IDictionary<TKey, TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<KeyValuePair<TKey, TValue?>>> writeConverter = null!;

        private static KeyValuePair<TKey, TValue?> Getter(IEnumerator<KeyValuePair<TKey, TValue?>> parent) => parent.Current;
        private static void Setter(IDictionary<TKey, TValue?> parent, KeyValuePair<TKey, TValue?> value) => parent.Add(value.Key, value.Value);

        protected override sealed void Setup()
        {
            var enumerableType = TypeAnalyzer<KeyValuePair<TKey, TValue?>>.GetTypeDetail();

            readConverter = ByteConverterFactory<IDictionary<TKey, TValue?>>.Get(enumerableType, null, null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue?>>>.Get(enumerableType, null, Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            if (state.Current.NullFlags && !state.Current.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }
            }

            IDictionary<TKey, TValue?> dictionary;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32Nullable(false, out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    state.Current.HasNullChecked = true;
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    dictionary = new Dictionary<TKey, TValue?>();
                    value = (TDictionary?)dictionary;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    dictionary = new DictionaryCounter();
                }
            }
            else
            {
                dictionary = (IDictionary<TKey, TValue?>)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (TDictionary?)state.Current.Object;
                else
                    value = default;
            }

            if (dictionary.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                state.PushFrame(true);
                var read = readConverter.TryReadFromParent(ref reader, ref state, dictionary);
                if (!read)
                {
                    state.Current.HasNullChecked = true;
                    state.Current.Object = dictionary;
                    return false;
                }

                if (dictionary.Count == state.Current.EnumerableLength.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, TDictionary? value)
        {
            if (state.Current.NullFlags && !state.Current.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out state.BytesNeeded))
                    {
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out state.BytesNeeded))
                {
                    return false;
                }
            }

            if (value == null)
                throw new InvalidOperationException($"{nameof(ByteSerializer)} should not be in this state");

            IEnumerator<KeyValuePair<TKey, TValue?>> enumerator;

            if (state.Current.Object is null)
            {
                var collection = (IReadOnlyCollection<KeyValuePair<TKey, TValue?>>)value;

                var length = collection.Count;

                if (!writer.TryWrite(length, out state.BytesNeeded))
                {
                    state.Current.HasWrittenIsNull = true;
                    return false;
                }
                if (length == 0)
                {
                    return true;
                }

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue?>>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                state.PushFrame(true);
                var write = writeConverter.TryWriteFromParent(ref writer, ref state, enumerator);
                if (!write)
                {
                    state.Current.HasWrittenIsNull = true;
                    state.Current.Object = enumerator;
                    state.Current.EnumeratorInProgress = true;
                    return false;
                }
                state.Current.EnumeratorInProgress = false;
            }

            return true;
        }

        private sealed class DictionaryCounter : IDictionary<TKey, TValue?>
        {
            private int count;
            public int Count => count;

            public TValue? this[TKey key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void Add(TKey key, TValue? value)
            {
                count++;
            }

            public void Add(KeyValuePair<TKey, TValue?> item)
            {
                count++;
            }

            public ICollection<TKey> Keys => throw new NotImplementedException();
            public ICollection<TValue?> Values => throw new NotImplementedException();
            public bool IsReadOnly => throw new NotImplementedException();
            public void Clear() => throw new NotImplementedException();
            public bool Contains(KeyValuePair<TKey, TValue?> item) => throw new NotImplementedException();
            public bool ContainsKey(TKey key) => throw new NotImplementedException();
            public void CopyTo(KeyValuePair<TKey, TValue?>[] array, int arrayIndex) => throw new NotImplementedException();
            public IEnumerator<KeyValuePair<TKey, TValue?>> GetEnumerator() => throw new NotImplementedException();
            public bool Remove(TKey key) => throw new NotImplementedException();
            public bool Remove(KeyValuePair<TKey, TValue?> item) => throw new NotImplementedException();
#if NET6_0_OR_GREATER
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value) => throw new NotImplementedException();
#else
            public bool TryGetValue(TKey key, out TValue? value) => throw new NotImplementedException();
#endif
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}