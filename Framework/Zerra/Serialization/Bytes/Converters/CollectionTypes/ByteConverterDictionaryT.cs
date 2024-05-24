// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Zerra.IO;
using Zerra.Reflection;

namespace Zerra.Serialization
{
    internal sealed class ByteConverterDictionaryT<TParent, TDictionary, TKey, TValue> : ByteConverter<TParent, TDictionary>
        where TKey : notnull
    {
        private ByteConverter<IDictionary<TKey, TValue?>> readConverter = null!;
        private ByteConverter<IEnumerator<KeyValuePair<TKey, TValue?>>> writeConverter = null!;

        private bool valueIsNullable;

        public override void Setup()
        {
            var enumerableType = TypeAnalyzer<KeyValuePair<TKey, TValue?>>.GetTypeDetail();

            Action<IDictionary<TKey, TValue?>, KeyValuePair<TKey, TValue?>> setter = (parent, value) => parent.Add(value.Key, value.Value);
            var readConverterRoot = ByteConverterFactory<IDictionary<TKey, TValue?>>.Get(options, enumerableType, null, null, setter);
            readConverter = ByteConverterFactory<IDictionary<TKey, TValue?>>.GetMayNeedTypeInfo(options, enumerableType, readConverterRoot);

            Func<IEnumerator<KeyValuePair<TKey, TValue?>>, KeyValuePair<TKey, TValue?>> getter = (parent) => parent.Current;
            var writeConverterRoot = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue?>>>.Get(options, enumerableType, null, getter, null);
            writeConverter = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue?>>>.GetMayNeedTypeInfo(options, enumerableType, writeConverterRoot);

            var valueTypeDetail = typeDetail.InnerTypeDetails[0].InnerTypeDetails[1];
            valueIsNullable = !valueTypeDetail.Type.IsValueType || valueTypeDetail.InnerTypeDetails[0].IsNullable;
        }

        protected override bool Read(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasNullChecked)
            {
                if (!reader.TryReadIsNull(out var isNull, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }

                if (isNull)
                {
                    value = default;
                    return true;
                }

                state.CurrentFrame.HasNullChecked = true;
            }

            IDictionary<TKey, TValue?> dictionary;

            int length;
            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                if (!reader.TryReadInt32(out length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    value = default;
                    return false;
                }

                state.CurrentFrame.EnumerableLength = length;

                if (!state.CurrentFrame.DrainBytes)
                {
                    dictionary = new Dictionary<TKey, TValue?>();
                    value = (TDictionary?)dictionary;
                    state.CurrentFrame.ResultObject = dictionary;
                    if (length == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (length == 0)
                        return true;
                    dictionary = new DictionaryCounter();
                    state.CurrentFrame.ResultObject = dictionary;
                }
            }
            else
            {
                dictionary = (IDictionary<TKey, TValue?>)state.CurrentFrame.ResultObject!;
                if (!state.CurrentFrame.DrainBytes)
                    value = (TDictionary?)state.CurrentFrame.ResultObject;
                else
                    value = default;
            }

            length = state.CurrentFrame.EnumerableLength.Value;
            if (dictionary.Count == length)
                return true;

            for (; ; )
            {
                state.PushFrame(readConverter, valueIsNullable, dictionary);
                var read = readConverter.Read(ref reader, ref state, dictionary);
                if (!read)
                    return false;

                if (dictionary.Count == length)
                    return true;
            }
        }

        protected override bool Write(ref ByteWriter writer, ref WriteState state, TDictionary? value)
        {
            int sizeNeeded;
            if (state.CurrentFrame.NullFlags && !state.CurrentFrame.HasWrittenIsNull)
            {
                if (value == null)
                {
                    if (!writer.TryWriteNull(out sizeNeeded))
                    {
                        state.BytesNeeded = sizeNeeded;
                        return false;
                    }
                    return true;
                }
                if (!writer.TryWriteNotNull(out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                state.CurrentFrame.HasWrittenIsNull = true;
            }

            IEnumerator<KeyValuePair<TKey, TValue?>> enumerator;

            if (!state.CurrentFrame.EnumerableLength.HasValue)
            {
                var collection = (IReadOnlyCollection<KeyValuePair<TKey, TValue?>>)value;

                var length = collection.Count;

                if (!writer.TryWrite(length, out sizeNeeded))
                {
                    state.BytesNeeded = sizeNeeded;
                    return false;
                }
                if (length == 0)
                {
                    return true;
                }
                state.CurrentFrame.Object = length;

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue?>>)state.CurrentFrame.Object!;
            }

            while (state.CurrentFrame.EnumeratorInProgress || enumerator.MoveNext())
            {
                state.CurrentFrame.EnumeratorInProgress = true;

                state.PushFrame(writeConverter, valueIsNullable, value);
                var write = writeConverter.Write(ref writer, ref state, enumerator);
                if (!write)
                    return false;

                state.CurrentFrame.EnumeratorInProgress = false;
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
#if NETSTANDARD2_0
            public bool TryGetValue(TKey key, out TValue? value) => throw new NotImplementedException();
#else
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue? value) => throw new NotImplementedException();
#endif
            IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }
    }
}