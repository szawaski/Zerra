// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using Zerra.Reflection;
using Zerra.Serialization.Bytes.IO;
using Zerra.Serialization.Bytes.State;

namespace Zerra.Serialization.Bytes.Converters.Collections.Dictionaries
{
    internal sealed class ByteConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue> : ByteConverter<TParent, TDictionary>
        where TKey : notnull
    {
        private ByteConverter<IDictionary<TKey, TValue>> readConverter = null!;
        private ByteConverter<IEnumerator<KeyValuePair<TKey, TValue>>> writeConverter = null!;

        private static KeyValuePair<TKey, TValue> Getter(IEnumerator<KeyValuePair<TKey, TValue>> parent) => parent.Current;
        private static void Setter(IDictionary<TKey, TValue> parent, KeyValuePair<TKey, TValue> value) => parent.Add(value.Key, value.Value);

        protected override sealed void Setup()
        {
            var keyValuePairTypeDetail = TypeAnalyzer<KeyValuePair<TKey, TValue>>.GetTypeDetail();
            readConverter = ByteConverterFactory<IDictionary<TKey, TValue>>.Get(keyValuePairTypeDetail, nameof(ByteConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), null, Setter);
            writeConverter = ByteConverterFactory<IEnumerator<KeyValuePair<TKey, TValue>>>.Get(keyValuePairTypeDetail, nameof(ByteConverterIDictionaryTOfT<TParent, TDictionary, TKey, TValue>), Getter, null);
        }

        protected override sealed bool TryReadValue(ref ByteReader reader, ref ReadState state, out TDictionary? value)
        {
            IDictionary<TKey, TValue> dictionary;

            if (!state.Current.EnumerableLength.HasValue)
            {
                if (!reader.TryRead(out state.Current.EnumerableLength, out state.BytesNeeded))
                {
                    value = default;
                    return false;
                }

                if (!state.Current.DrainBytes)
                {
                    if (typeDetail.Type.IsInterface || typeDetail.Type.Name == "Dictionary`2")
                    {
                        dictionary = new Dictionary<TKey, TValue>();
                        value = (TDictionary?)dictionary;
                    }
                    else
                    {
                        value = typeDetail.Creator();
                        dictionary = (IDictionary<TKey, TValue>)value!;
                    }
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                }
                else
                {
                    value = default;
                    if (state.Current.EnumerableLength!.Value == 0)
                        return true;
                    dictionary = new IDictionaryTCounter<TKey, TValue>();
                }
            }
            else
            {
                dictionary = (IDictionary<TKey, TValue>)state.Current.Object!;
                if (!state.Current.DrainBytes)
                    value = (TDictionary?)state.Current.Object;
                else
                    value = default;
            }

            if (dictionary.Count == state.Current.EnumerableLength.Value)
                return true;

            for (; ; )
            {
                if (!readConverter.TryReadFromParent(ref reader, ref state, dictionary, true))
                {
                    state.Current.Object = dictionary;
                    return false;
                }

                if (dictionary.Count == state.Current.EnumerableLength!.Value)
                    return true;
            }
        }

        protected override sealed bool TryWriteValue(ref ByteWriter writer, ref WriteState state, in TDictionary value)
        {
            IEnumerator<KeyValuePair<TKey, TValue>> enumerator;

            if (state.Current.Object is null)
            {
                var collection = (ICollection<KeyValuePair<TKey, TValue>>)value!;

                if (!writer.TryWrite(collection.Count, out state.BytesNeeded))
                {
                    return false;
                }
                if (collection.Count == 0)
                {
                    return true;
                }

                enumerator = collection.GetEnumerator();
            }
            else
            {
                enumerator = (IEnumerator<KeyValuePair<TKey, TValue>>)state.Current.Object!;
            }

            while (state.Current.EnumeratorInProgress || enumerator.MoveNext())
            {
                if (!writeConverter.TryWriteFromParent(ref writer, ref state, enumerator, true))
                {
                    state.Current.Object = enumerator;
                    state.Current.EnumeratorInProgress = true;
                    return false;
                }
                state.Current.EnumeratorInProgress = false;
            }

            return true;
        }
    }
}